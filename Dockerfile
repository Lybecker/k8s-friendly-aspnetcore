FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
ENV ASPNETCORE_URLS http://+:5000;https://+:5001
EXPOSE 5000
EXPOSE 5001

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