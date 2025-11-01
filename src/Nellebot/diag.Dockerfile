FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN dotnet tool install -g dotnet-counters && \
    dotnet tool install -g dotnet-monitor && \
    dotnet tool install -g dotnet-trace && \
    dotnet tool install -g dotnet-dump && \
    dotnet tool install -g dotnet-stack

ENV PATH="/root/.dotnet/tools:$PATH"

ENTRYPOINT ["tail", "-f", "/dev/null"]