FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/GameRecommender/GameRecommender.csproj", "GameRecommender/"]
RUN dotnet restore "GameRecommender/GameRecommender.csproj"
COPY . .
WORKDIR "/src/GameRecommender"
RUN dotnet build "GameRecommender.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameRecommender.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameRecommender.dll"] 