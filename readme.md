Skeleton sample project for a Docker container friendly ASP.NET core application
 - Dockerfile using Microsoft Container Registry (MCR) 
 - Dockerfile exposing both HTTP and HTTPS ports
 - Helm chart
 - Mount HTTPS certificate via volume and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Gracefull shutdown with SIGTERM - giving the application time to cleanup connections etc.

## Built Docker image
Run command in the directory where the Dockerfile is located
```
docker build --tag myaspnetcoreapp:v1 .
```


## Volume mount HTTPS certificate
Create a self-signed certificate and export it to a file:

```
dotnet dev-certs https -v -ep c:\cert\aspnetcore-cert.pfx -p createyourownpassword
```

> Certificate password is optional

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
    -v c:\cert\:/root/.dotnet/https myaspnetcoreapp:v1
``` 

## References
 - [Developing a Dockerized Asp.Net Core Application Using Visual Studio Code](https://medium.com/@waelkdouh/developing-a-dockerized-asp-net-core-application-using-visual-studio-code-6ccfc59d6f6)