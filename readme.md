Skeleton sample project for a Docker container friendly ASP.NET core application
 - Dockerfile using [Microsoft Container Registry (MCR)](https://azure.microsoft.com/en-us/blog/microsoft-syndicates-container-catalog/) base images
 - Dockerfile exposing both HTTP and HTTPS custom ports overwritten default [Kestrel settings](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.2)
 - Helm chart for deployment on Kubernetes cluster
 - Mount HTTPS (TLS/SSL) certificate via volume and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Gracefull shutdown with SIGTERM and SIGKILL - giving the application time to cleanup connections etc.

# Install on Kubernetes cluster
Assuming you have a cluster, kubectl and [helm](https://helm.sh) configured.

1. Clone this repository
2. Generate self-signed certificate
    ```bash
    dotnet dev-certs https -v -ep .\HelmCharts\dotnetcoredocker\templates\_aspnetcore-cert.pfx -p createyourownpassword
    ```
3. Install via Helm
    ```bash
    helm install helmchart\dotnetcoredocker\ --name nameofdeployment
    ```
4. Test the endpoint
    ```bash
    curl --insecure -i https://<ip-address>/api/values
    ```

# Step-by-step walk-through
Clone this repo
## Built Docker image
Run command in the directory where the Dockerfile is located
```bash
docker build --tag myaspnetcoreimage:v1 .
```

> Requires [Docker installed and running](https://docs.docker.com/install/) locally

### Volume mount HTTPS certificate
Create a self-signed certificate and export it to a file:

```bash
dotnet dev-certs https -v -ep .\HelmCharts\dotnetcoredocker\templates\_aspnetcore-cert.pfx -p createyourownpassword
```

### Try to run the container locally with self-signed certificate

Thi is how we will run the container 
- exposing port 5000 & 5001- mount volumne .\HelmCharts\dotnetcoredocker\templates\ to /root/.dotnet/https
- Set `Kestrel__Certificates__Default__Path` path to the certificate location
- Set `Kestrel__Certificates__Default__Password` value of the password
- mount volumne c:\cert\ to /root/.dotnet/https

With the following command
```bash
docker run 
    -p 5000:5000 -p 5001:5001 
    -e Kestrel__Certificates__Default__Path=/root/.dotnet/https/aspnetcore-cert.pfx 
    -e Kestrel__Certificates__Default__Password=createyourownpassword 
    -v .\HelmCharts\dotnetcoredocker\templates\:/root/.dotnet/https 
    myaspnetcoreimage:v1
``` 

## Kubernetes liveness and readiness probes
As a self-signed certificate is used, Kubernetes probes will fail, so the `app.UseHttpsRedirection()` is removed in the `Startup` class.

## shutdown
The default timeout is 5 seconds, but we can increase it by calling the [`UseShutdownTimeout`](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/web-host?view=aspnetcore-2.2#shutdown-timeout) extension method on the WebHostBuilder in our Program.Main() method or configuring with the environment variable `ASPNETCORE_SHUTDOWNTIMEOUTSECONDS`.

https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2#iapplicationlifetime-interface


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