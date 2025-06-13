# Use the official .NET 9 SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy everything first
COPY . .

# Restore dependencies (using capital URL)
RUN dotnet restore "URLShortener.Api/URLShortener.Api.csproj"

# Build the application
WORKDIR /src/URLShortener.Api
RUN dotnet build "URLShortener.Api.csproj" -c Release -o /app/build

# Publish the application  
FROM build AS publish
WORKDIR /src/URLShortener.Api
RUN dotnet publish "URLShortener.Api.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "URLShortener.Api.dll"]