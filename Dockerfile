# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY web.sln .
COPY TaskManagerMVC/TaskManagerMVC.csproj TaskManagerMVC/

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY TaskManagerMVC/ TaskManagerMVC/
RUN dotnet publish TaskManagerMVC/TaskManagerMVC.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TaskManagerMVC.dll"]
