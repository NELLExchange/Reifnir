FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN mkdir /keydata && \
    chown $APP_UID:$APP_UID /keydata
    
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
RUN mkdir /keydata && \
    chown $APP_UID:$APP_UID /keydata
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["src/Nellebot/Nellebot.csproj", "Nellebot/"]
COPY ["src/Nellebot.Common/Nellebot.Common.csproj", "Nellebot.Common/"]
COPY ["src/Nellebot.Data/Nellebot.Data.csproj", "Nellebot.Data/"]
RUN dotnet restore "./Nellebot/Nellebot.csproj"
COPY ["Nellebot.sln", "."]
COPY ["stylecop.json", "."]
COPY src .
RUN dotnet build "./Nellebot/Nellebot.csproj" -c $BUILD_CONFIGURATION --no-restore
USER $APP_UID
ENTRYPOINT ["dotnet", "run", "--project", "Nellebot/Nellebot.csproj", "--no-build"]