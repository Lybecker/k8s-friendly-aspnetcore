Skeleton sample project for a Docker container friendly ASP.NET core application
 - Dockerfile using Microsoft Container Registry (MCR) 
 - Dockerfile exposing both HTTP and HTTPS ports
 - Mount HTTPS certificate via volume and use it with Kestrel
 - Health endpoint for probes - used for warmup process and to determind if process is responsive
 - Gracefull shutdown with SIGTERM - giving the application time to close connections etc.

## References
 - [Developing a Dockerized Asp.Net Core Application Using Visual Studio Code](https://medium.com/@waelkdouh/developing-a-dockerized-asp-net-core-application-using-visual-studio-code-6ccfc59d6f6)