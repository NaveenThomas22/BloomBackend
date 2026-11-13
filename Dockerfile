
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source code and project files
COPY . .

# Restore dependencies
RUN dotnet restore Bloom.API/Bloom.API.csproj

# Publish API
RUN dotnet publish Bloom.API/Bloom.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Railway provides PORT dynamically
ENTRYPOINT ["sh", "-c", "dotnet Bloom.API.dll --urls http://0.0.0.0:${PORT:-8080}"]
