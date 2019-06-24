Sample project for a Kubernetes friendly ASP.NET core application
 - Dockerfile using [Microsoft Container Registry (MCR)](https://azure.microsoft.com/en-us/blog/microsoft-syndicates-container-catalog/) base images
 - Dockerfile exposing both HTTP and HTTPS custom ports overwritten default [Kestrel settings](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.2)
 - Helm chart for deployment on Kubernetes cluster
 - Mount HTTPS (TLS/SSL) certificate via volume secret, certificate password as secret and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Graceful shutdown with SIGTERM and SIGKILL - giving the application time to cleanup connections etc.
 - Specify the compute resources needed to run and maximum consumption

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
docker run --name mmyk8sfriendlyaspnetcorecontainer 
    -p 5000:5000 -p 5001:5001 
    -e Kestrel__Certificates__Default__Path=/root/.dotnet/https/_aspnetcore-cert.pfx 
    -e Kestrel__Certificates__Default__Password=createyourownpassword 
    -v .\HelmChart\k8sfriendlyaspnetcore\templates\:/root/.dotnet/https 
    anderslybecker/k8sfriendlyaspnetcore:v1
``` 
> Mounting a volume on Windows does not allow usage of relative paths. Modify the volume mount path to e.g. `c:\Code\k8s-friendly-aspnetcore\HelmChart\k8sfriendlyaspnetcore\templates\`

> Expose both HTTP and HTTPS on non-standard ports. Normally port 80 and 443 is used. This is just to show what is required to use other ports.

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

## Graceful shutdown
Sometimes it is required to buy some time for shutdown of a process. Perhaps a transaction needs to be completed or a connections needs to be properly closed. That is why we need the ability to shutdown gracefully.

ASP.NET Core exposes [application life-cycle events](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2#iapplicationlifetime-interface) for this purpose.

The `LifetimeEventsHostedService` class implements the ASP.NET Core application life-cycle events and simulates a process requiring extra time to shutdown.

> The default allowed shutdown timeout is 5 seconds, but we can increase it by calling the [`UseShutdownTimeout`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-2.2#shutdown-timeout) extension method on the WebHostBuilder in Program.Main() method or configuring with the environment variable `ASPNETCORE_SHUTDOWNTIMEOUTSECONDS`.

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