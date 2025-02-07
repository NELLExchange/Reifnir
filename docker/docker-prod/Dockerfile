FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Nellebot/Nellebot.csproj", "Nellebot/"]
COPY ["Nellebot.Common/Nellebot.Common.csproj", "Nellebot.Common/"]
COPY ["Nellebot.Data/Nellebot.Data.csproj", "Nellebot.Data/"]
COPY ["Nellebot.Data.Migrations/Nellebot.Data.Migrations.csproj", "Nellebot.Data.Migrations/"]
COPY [".config/dotnet-tools.json", ".config/"]
RUN dotnet restore "./Nellebot/Nellebot.csproj"
RUN dotnet restore "./Nellebot.Data.Migrations/Nellebot.Data.Migrations.csproj"
RUN dotnet tool restore
COPY . .
RUN dotnet build "./Nellebot/Nellebot.csproj" --no-restore -c $BUILD_CONFIGURATION
RUN dotnet build "./Nellebot.Data.Migrations/Nellebot.Data.Migrations.csproj" --no-restore -c $BUILD_CONFIGURATION

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nellebot/Nellebot.csproj" --no-build -c $BUILD_CONFIGURATION -o /output/publish /p:UseAppHost=false

FROM build AS migrations
ARG BUILD_CONFIGURATION=Release
RUN dotnet ef migrations script --no-build --configuration $BUILD_CONFIGURATION --idempotent -p Nellebot.Data.Migrations -o /output/migrations/database_migration.sql
COPY --from=build /src/scripts/nellebot-backup-db.sh /output/migrations/

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
RUN mkdir /keydata && \
    chown $APP_UID:$APP_UID /keydata
WORKDIR /app
COPY --from=publish /output/publish .
COPY --from=migrations /output/migrations ./migrations/
USER $APP_UID
ENTRYPOINT ["dotnet", "Nellebot.dll"]