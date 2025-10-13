# Use the official .NET 9 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["AmplePack/AmplePack.csproj", "AmplePack/"]
COPY ["AmplePack.Tests/AmplePack.Tests.csproj", "AmplePack.Tests/"]

# Restore dependencies
RUN dotnet restore "AmplePack/AmplePack.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/AmplePack"
RUN dotnet build "AmplePack.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "AmplePack.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install required dependencies for SQLite
RUN apt-get update && apt-get install -y sqlite3 libsqlite3-dev && rm -rf /var/lib/apt/lists/*

# Create directory for database
RUN mkdir -p /app/data

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Create a non-root user
RUN groupadd -r amplepack && useradd -r -g amplepack amplepack
RUN chown -R amplepack:amplepack /app
USER amplepack

ENTRYPOINT ["dotnet", "AmplePack.dll"]