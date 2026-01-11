# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy csproj and restore
COPY ["WorthEngine.Api/WorthEngine.Api.csproj", "WorthEngine.Api/"]
COPY ["WorthEngine.Services/WorthEngine.Services.csproj", "WorthEngine.Services/"]
COPY ["WorthEngine.Core/WorthEngine.Core.csproj", "WorthEngine.Core/"]
RUN dotnet restore "WorthEngine.Api/WorthEngine.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/WorthEngine.Api"
RUN dotnet publish -c Release -o /app/publish

# Serve Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Render Config
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
# Workstation GC is critical for low memory
ENV DOTNET_gcServer=0
# Disable Globalization to save ~30MB+ RAM (Critical for free tier)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
# Remove explicit heap limit as it was causing Init failures
# ENV DOTNET_GCHeapHardLimit=128M

EXPOSE 8080
ENTRYPOINT ["dotnet", "WorthEngine.Api.dll"]
