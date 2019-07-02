FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app

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

# Restore packages
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
COPY ["k8s-friendly-aspnetcore.csproj", "./"]
RUN dotnet restore "./k8s-friendly-aspnetcore.csproj"
COPY . .
# WORKDIR "/src/."
# RUN dotnet build "k8s-friendly-aspnetcore.csproj" -c Release -o /app

# Pack the application and its dependencies into a folder /app
FROM build AS publish
RUN dotnet publish "k8s-friendly-aspnetcore.csproj" -c Release -o /app

# Copy the publish output into the base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app .

# Change to the app user.
USER ${USERNAME}

ENTRYPOINT ["dotnet", "k8s-friendly-aspnetcore.dll"]