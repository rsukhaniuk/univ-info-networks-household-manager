# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY HouseholdManager.sln .

# Copy all project files for restore
COPY backend/src/HouseholdManager.Domain/HouseholdManager.Domain.csproj backend/src/HouseholdManager.Domain/
COPY backend/src/HouseholdManager.Application/HouseholdManager.Application.csproj backend/src/HouseholdManager.Application/
COPY backend/src/HouseholdManager.Infrastructure/HouseholdManager.Infrastructure.csproj backend/src/HouseholdManager.Infrastructure/
COPY backend/src/HouseholdManager.Api/HouseholdManager.Api.csproj backend/src/HouseholdManager.Api/

# Restore dependencies
RUN dotnet restore backend/src/HouseholdManager.Api/HouseholdManager.Api.csproj

# Copy all source code
COPY backend/src/ backend/src/

# Build
WORKDIR /src/backend/src/HouseholdManager.Api
RUN dotnet build -c Release -o /app/build

# Publish
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads

# Create directory for SSL certificates (used in local development via docker-compose)
RUN mkdir -p /app/certs

# Default: HTTP on port 8080 (production - AWS App Runner handles HTTPS termination)
# Local development: docker-compose.yml overrides these with HTTPS settings
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HouseholdManager.Api.dll"]
