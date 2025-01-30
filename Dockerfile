FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/GameRecommender/GameRecommender.csproj", "GameRecommender/"]
RUN dotnet restore "GameRecommender/GameRecommender.csproj"

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy the rest of the code
COPY ["src/GameRecommender/", "GameRecommender/"]

# Install libman CLI and restore client-side libraries
RUN dotnet tool install -g Microsoft.Web.LibraryManager.CLI
RUN cd GameRecommender && libman restore

# Build and publish
RUN dotnet publish "GameRecommender/GameRecommender.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Debug: Check if wwwroot is in the publish output
RUN ls -la /app/publish/wwwroot || echo "wwwroot not found in publish output"

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install SQLite and .NET SDK (needed for EF tools)
RUN apt-get update && \
    apt-get install -y sqlite3 wget && \
    wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-8.0 && \
    rm -rf /var/lib/apt/lists/* packages-microsoft-prod.deb

# Create directories for data and secrets
RUN mkdir -p /app/keys && \
    mkdir -p /app/db && \
    mkdir -p /app/wwwroot

# Copy published files and set permissions
COPY --from=build /app/publish .
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
RUN chmod -R 755 /app/wwwroot && \
    chmod -R 700 /app/keys /app/db

# Set environment variables
ENV ASPNETCORE_URLS=http://+:2002
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DataProtection__Path=/app/keys
ENV DataProtection__ApplicationName=GameRecommender
ENV ConnectionStrings__DefaultConnection="Data Source=/app/db/gamerecommender.db"
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy and set up the startup script
COPY ["src/GameRecommender/docker-entrypoint.sh", "/app/"]
RUN chmod +x /app/docker-entrypoint.sh

# Expose port
EXPOSE 2002

ENTRYPOINT ["/app/docker-entrypoint.sh"] 