# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Render Config
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0
# Limit GC Heap to avoid OOM on free tier (512MB RAM available)
ENV DOTNET_GCHeapHardLimit=300M

EXPOSE 8080
ENTRYPOINT ["dotnet", "WorthEngine.Api.dll"]
