FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
COPY ["dotnetcore-docker.csproj", "./"]
RUN dotnet restore "./dotnetcore-docker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "dotnetcore-docker.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "dotnetcore-docker.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "dotnetcore-docker.dll"]