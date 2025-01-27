FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
RUN mkdir /data
EXPOSE 2002

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
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "GameRecommender.dll"] 