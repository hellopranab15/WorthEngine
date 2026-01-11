# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "WorthEngine.Api.dll"]
