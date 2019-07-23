Sample project for a Kubernetes friendly ASP.NET core application
 - Dockerfile using [Microsoft Container Registry (MCR)](https://azure.microsoft.com/en-us/blog/microsoft-syndicates-container-catalog/) base images
 - Dockerfile exposing both HTTP and HTTPS custom ports overwritten default [Kestrel settings](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.2)
 - Helm chart for deployment on Kubernetes cluster
 - Mount HTTPS (TLS/SSL) certificate via volume secret, certificate password as secret and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Graceful shutdown with SIGTERM and SIGKILL - giving the application time to cleanup connections etc.
 - Specify the compute resources needed to run and maximum consumption
 - Execute as unprivileged account

[![Build Status](https://lybecker.visualstudio.com/Microsoft/_apis/build/status/Lybecker.k8s-friendly-aspnetcore?branchName=master)](https://lybecker.visualstudio.com/Microsoft/_build/latest?definitionId=23&branchName=master)

# Test on Kubernetes cluster
Assuming you have a cluster, kubectl and [helm](https://helm.sh) configured.

> Execute the commands from the repository base folder.

1. Clone this repository
2. Generate self-signed certificate
    ```bash
    dotnet dev-certs https -v -ep .\helmchart\k8sfriendlyaspnetcore\templates\_aspnetcore-cert.pfx -p createyourownpassword
    ```
3. Install via Helm
    ```bash
    helm install helmchart\k8sfriendlyaspnetcore\ --name nameofdeployment
    ```
    The Kubernetes deployment will use image [anderslybecker/k8sfriendlyaspnetcore](https://hub.docker.com/r/anderslybecker/k8sfriendlyaspnetcore) from Docker Hub. Overwrite like this:
    ```bash
    helm install helmchart\k8sfriendlyaspnetcore\ --name nameofdeployment --set image.repository <image name>
    ```
4. Test the endpoint
    ```bash
    curl --insecure -i https://<ip-address>/api/values
    ```
    Replacing the `<ip-address>` with the external IP from the Kubernetes cluster. Check with command 
    ```bash
    helm status nameofdeployment
    ```
5. Kill the pod and observe the behavior
    Observe the process will gracefully shutdown doing any clean-up before terminating. 

    Observice the Pod logs with this command:
    ```bash
    kubectl logs --follow <name of the pod>
    ```
    > The Pod name will be something like `nameofdeployment-k8sfriendlyaspnetcore-bdfd6b6c7-cl6jm`. Check with helm status command as the previous step.

    > Notice that you can see the Kubernetes health probes polling every few seconds.

    Kill the Pod and watch the logs
    ```bash
    kubectl kill <name of the pod>
    ```

    > Kubernetes will automatically create a new Pod if it is terminated. Verify bu testing the endpoint with curl and check the helm status.

To remoce and purge the helm installation from the Kubernetes cluster run:
```bash
helm del --purge nameofdeployment
```

# Details

## Built Docker image
Run command in the `src` folder where the Dockerfile is located
```bash
docker build --tag k8sfriendlyaspnetcore:v1 .
```

> Requires [Docker installed and running](https://docs.docker.com/install/) locally

## Volume mount HTTPS certificate
Create a self-signed certificate and export it to a file:

```bash
dotnet dev-certs https -v -ep .\HelmChart\k8sfriendlyaspnetcore\templates\_aspnetcore-cert.pfx -p createyourownpassword
```

> The certificate is stored in the Helm templates folder, as Helm Charts does not currently support files as parameters (issue [#3276](https://github.com/helm/helm/issues/3276)).

## Try to run the container locally with self-signed certificate

This is how we will run the container 
- exposing port 5000 & 5001
- Set `Kestrel__Certificates__Default__Path` path to the certificate location
- Set `Kestrel__Certificates__Default__Password` value of the password
- mount volume with certificate .\HelmChart\k8sfriendlyaspnetcore\templates\ to /root/.dotnet/https

With the following command
```bash
docker run --name myk8sfriendlyaspnetcorecontainer 
    -p 5000:5000 -p 5001:5001 
    -e Kestrel__Certificates__Default__Path=/certs/_aspnetcore-cert.pfx 
    -e Kestrel__Certificates__Default__Password=createyourownpassword 
    -v .\HelmChart\k8sfriendlyaspnetcore\templates\:/certs 
    anderslybecker/k8sfriendlyaspnetcore:v1
``` 
> Mounting a volume on Windows does not allow usage of relative paths. Modify the volume mount path to e.g. `c:\code\k8s-friendly-aspnetcore\HelmChart\k8sfriendlyaspnetcore\templates\`

> Expose both HTTP and HTTPS on non-standard ports. Normally port 80 and 443 is used, but running the ASP.NET process as an unprivileged non-root account requires to use ports above 1024.

## Kubernetes liveness and readiness probes
A custom `HealthController` is used for health monitoring. You could use the build-in [health monitoring in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2).

>As a self-signed certificate is used, Kubernetes probes will fail, so the `app.UseHttpsRedirection()` is removed in the `Startup` class. Resulting in a HTTP request will not be redirected to HTTPS.

The Kubernetes deployment specifics the endpoints and ports
```dockerfile
livenessProbe:
  httpGet:
    path: /api/health
    port: {{ .Values.containerPort }} 
readinessProbe:
  httpGet:
    path: /api/health/ready
    port: {{ .Values.containerPort }}
```
> The `{{ .Values.containerPort }}` variable is specified in the Helm values file.

See [Kubernetes deployment example](/HelmChart/k8sfriendlyaspnetcore/templates/deployment.yaml).

## Graceful shutdown
Sometimes it is required to buy some time for shutdown of a process. Perhaps a transaction needs to be completed or a connections needs to be properly closed. That is why we need the ability to shutdown gracefully.

ASP.NET Core exposes [application life-cycle events](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2#iapplicationlifetime-interface) for this purpose. 

The `LifetimeEventsHostedService` class implements the ASP.NET Core application life-cycle events and simulates a process requiring extra time to shutdown.

> The default allowed shutdown timeout is 5 seconds, but we can increase it by calling the [`UseShutdownTimeout`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-2.2#shutdown-timeout) extension method on the WebHostBuilder in Program.Main() method or configuring with the environment variable `ASPNETCORE_SHUTDOWNTIMEOUTSECONDS`.

> The default max grace termination period for a Kubernetes Pod is 30 sec. But it can be changed via the terminationGracePeriodSeconds setting.

### Kubernetes termination lifecycle
How the Kubernetes termination lifecycle works:

1. Pod is set to the “Terminating” State and removed from the endpoints list of all Services
2. [preStop](https://kubernetes.io/docs/concepts/containers/container-lifecycle-hooks/) Hook is executed 
3. SIGTERM signal is sent to the pod
4. Kubernetes waits for a grace period
5. SIGKILL signal is sent to pod, and the pod is removed

### .NET Core 3.0 changes

For .NET Core 3.0 the application life-cycle events class used in previous verison [IApplicationLifetime](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.hosting.iapplicationlifetime?view=aspnetcore-2.2) is depricated and you should use [IHostApplicationLifetime](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostapplicationlifetime?view=aspnetcore-3.0). It is just a drop-in replacement in the [`LifetimeEventsHostedService`](/src/LifetimeEventsHostedService.cs#L11) file.

## Compute resources needed and maximum
In the deployment file, the requested and maximum compute resources are specified:

```dockerfile
resources:
  requests:
    memory: "128Mi"
    cpu: "100m"
  limits:
    memory: "256Mi"
    cpu: "500m"
```
See [Kubernetes deployment example](/HelmChart/k8sfriendlyaspnetcore/templates/deployment.yaml).

## Execute as unprivileged account
By default a Docker container runs as root user (id: 0), which means the app inside the container can do anything inside the container. To adhere to the principle of least privilege, the app running insinde the container should be running in the context of an unprivileged non-root account.

To test if a container can run without any issues as an an unprivileged non-root account, try to run it with a random user ID (not 0 as it is root). It does not matter if the user ID exists on the host or in the container. It will override settings inside the `Dockerfile`:

```bash
docker run --user $((RANDOM+1)) <YOUR CONTAINER>
```

For at ASP.NET application an user with at least execute permissions is needed to execute the application.
Create the user like this in the `Dockerfile`:

```dockerfile
RUN groupadd -r grp &&\
    useradd -r -g grp -d /home/app -s /sbin/nologin -c "Docker image user" app
```
-r creates a [system account](https://linux.die.net/man/8/useradd).

By default a new user ID is created the system assigns the next available ID from the range of user IDs specified in the `login.defs` file.

If you want to reference the user and group ID in the Kubernetes Security Context, then specify the IDs:
```dockerfile
RUN groupadd -r 999 grp &&\
    useradd -r -u 999 -g grp -d /home/app -s /sbin/nologin -c "Docker image user" app
```

A complete Dockerfile looks like this:
```dockerfile
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2

# Declare ports above 1024 as an unprivileged non-root user cannot bind to > 1024
ENV ASPNETCORE_URLS http://+:5000;https://+:5001
EXPOSE 5000
EXPOSE 5001

ENV USERNAME=appuser
ENV GROUP=grp
ENV HOME=/home/${USERNAME}

RUN mkdir -p ${HOME}

# Create a group and an user (system account) which will execute the app
RUN groupadd -r ${GROUP} &&\
    useradd -r -g ${GROUP} -d ${HOME} -s /sbin/nologin -c "Docker image user" ${USERNAME}

# Setup the app directory
ENV APP_HOME=${HOME}/app
RUN mkdir ${APP_HOME}
WORKDIR ${APP_HOME}

# Copy in the application code
ADD . ${APP_HOME}

# Change the context to the app user
USER ${USERNAME}

ENTRYPOINT ["dotnet", "k8s-friendly-aspnetcore.dll"]
```
See an example of a [multi-stage build Dockerfile](/src/Dockerfile).

> A unprivileged none-root account cannot bind to ports below 1024, hence the default HTTP port 80 og HTTPS port 443 cannot be used.

In Kubernetes the Security Context controls how a Pod is excecuted. The Security Context can be configured both at Pod and Container level [Configure a Security Context](https://kubernetes.io/docs/tasks/configure-pod-container/security-context/).

```dockerfile
securityContext:
  runAsUser: 999
  runAsGroup: 999
  allowPrivilegeEscalation: false
```
See [Kubernetes deployment example](/HelmChart/k8sfriendlyaspnetcore/templates/deployment.yaml).


 `runAsUser` and `runAsGroup` option to specify the Linux user and group executing the process. It overrides the `USER` instruction of the `Dockerfile`. `AllowPrivilegeEscalation` controls whether a process can gain more privileges than its parent process.

### Alpine linux
To use the the Alpine-based Docker image available for .NET Core a small changes has to be applied, as the functions for adding users and groups has different names.

```dockerfile
RUN addgroup -S ${GROUP} && adduser -S ${USERNAME} -G ${GROUP} -h ${HOME} -s /sbin/nologin
```

See the [SimpleDockerfile.Alpine](/src/SimpleDockerfile.Alpine) file for a complete example.

> It is possible to use the Alpine Guest user (UID 405), if you do not want to create your own.

# Gotchas
- The base image from Microsoft Container Registry sets listen to port 80, but can be overwritten by setting `ASPNETCORE_URLS` environment variable in the Dockerfile
- Running ASP.Net Core on a Linux host will result in the warning 
    ```
    Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager 
    No XML encryptor configured. Key {61c34317-3cbe-4d98-ae83-b784e89f1320} may be persisted to storage in unencrypted form.
    ```
  It can easily be fixed by supplyying a [Data Protection key](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-2.2) in the `Startup` class

# References
 - [Developing a Dockerized Asp.Net Core Application Using Visual Studio Code](https://medium.com/@waelkdouh/developing-a-dockerized-asp-net-core-application-using-visual-studio-code-6ccfc59d6f6)
 - [Helm Chart Development Tips and Tricks](https://github.com/helm/helm/blob/master/docs/charts_tips_and_tricks.md)
 - [Managing ASP.NET Core App Settings on Kubernetes](https://anthonychu.ca/post/aspnet-core-appsettings-secrets-kubernetes/)
 - [Health checks in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-2.2)
 - [Graceful termination in Kubernetes with ASP.NET Core](https://blog.markvincze.com/graceful-termination-in-kubernetes-with-asp-net-core/#comment-4509101865)
 - [Understanding how uid and gid work in Docker containers](https://medium.com/@mccode/understanding-how-uid-and-gid-work-in-docker-containers-c37a01d01cf)
