# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy solution and project files
COPY *.sln .
COPY src/PassageLite.Api/*.csproj src/PassageLite.Api/
COPY src/PassageLite.Application/*.csproj src/PassageLite.Application/
COPY src/PassageLite.Domain/*.csproj src/PassageLite.Domain/
COPY src/PassageLite.Infrastructure/*.csproj src/PassageLite.Infrastructure/
COPY tests/PassageLite.Tests/*.csproj tests/PassageLite.Tests/

# Restore packages
RUN dotnet restore

# Copy source code
COPY src/ src/
COPY tests/ tests/

# Build and publish
WORKDIR /source/src/PassageLite.Api
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "PassageLite.Api.dll"]
