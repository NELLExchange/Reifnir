FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

RUN mkdir /keydata && \
    chown $APP_UID:$APP_UID /keydata 

USER $APP_UID
