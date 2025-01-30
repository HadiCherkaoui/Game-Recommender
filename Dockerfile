FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/GameRecommender/GameRecommender.csproj", "GameRecommender/"]
RUN dotnet restore "GameRecommender/GameRecommender.csproj"

# Copy the rest of the code
COPY ["src/GameRecommender/", "GameRecommender/"]

# Build and publish
RUN dotnet publish "GameRecommender/GameRecommender.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy published files and set permissions
COPY --from=build /app/publish .
RUN chown -R 1000:1000 /app \
    && chmod -R 755 /app/wwwroot

# Set environment variables
ENV ASPNETCORE_URLS=http://+:2002
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 2002

ENTRYPOINT ["dotnet", "GameRecommender.dll"] 