Skeleton sample project for a Docker container friendly ASP.NET core application
 - Dockerfile using [Microsoft Container Registry (MCR)](https://azure.microsoft.com/en-us/blog/microsoft-syndicates-container-catalog/) base images
 - Dockerfile exposing both HTTP and HTTPS custom ports overwritten default [Kestrel settings](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.2)
 - Helm chart
 - Mount HTTPS certificate via volume and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Gracefull shutdown with SIGTERM - giving the application time to cleanup connections etc.

## Built Docker image
Run command in the directory where the Dockerfile is located
```
docker build --tag myaspnetcoreimage:v1 .
```


## Volume mount HTTPS certificate
Create a self-signed certificate and export it to a file:

```
dotnet dev-certs https -v -ep c:\cert\aspnetcore-cert.pfx -p createyourownpassword
```

Run container 
- exposing port 5000 & 5001
- mount volumne c:\cert\ to /root/.dotnet/https
- Set `Kestrel__Certificates__Default__Path` path to the certificate location
- Set `Kestrel__Certificates__Default__Password` value of the password
- mount volumne c:\cert\ to /root/.dotnet/https

With the following command
```
docker run 
    -p 5000:5000 -p 5001:5001 
    -e Kestrel__Certificates__Default__Path=/root/.dotnet/https/aspnetcore-cert.pfx 
    -e Kestrel__Certificates__Default__Password=createyourownpassword 
    -v c:\cert\:/root/.dotnet/https 
    myaspnetcoreimage:v1
``` 

## Gotchas
- The base image from Microsoft Container Registry sets listen to port 80, but can be overwritten by setting `ASPNETCORE_URLS` environment variable in the Dockerfile
## References
 - [Developing a Dockerized Asp.Net Core Application Using Visual Studio Code](https://medium.com/@waelkdouh/developing-a-dockerized-asp-net-core-application-using-visual-studio-code-6ccfc59d6f6)