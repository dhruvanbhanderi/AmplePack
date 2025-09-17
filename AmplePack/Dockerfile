# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AmplePack.csproj", "./"]
RUN dotnet restore "AmplePack.csproj"
COPY . .
RUN dotnet build "AmplePack.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AmplePack.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AmplePack.dll"]