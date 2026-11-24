# syntax=docker/dockerfile:1

# Build on the SDK image, run on the smaller ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/VCare.Api/VCare.Api.csproj
RUN dotnet publish src/VCare.Api/VCare.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# TLS ends at the load balancer or ingress; the container serves plain HTTP.
# Configuration (connection string, Jwt, Smtp, Storage, Hosting) comes from
# environment variables, e.g. ConnectionStrings__Default. appsettings.json is
# not committed and not copied.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The runtime image's built-in non-root user.
USER $APP_UID

# Probes: GET /health/live (process up) and GET /health/ready (database reachable).
ENTRYPOINT ["dotnet", "VCare.Api.dll"]
