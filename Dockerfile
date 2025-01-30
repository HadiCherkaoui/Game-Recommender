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
COPY --from=build /app/publish .

# Ensure proper permissions for the app directory
RUN chown -R 1000:1000 /app

EXPOSE 2002

ENTRYPOINT ["dotnet", "GameRecommender.dll"] 