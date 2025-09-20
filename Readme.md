# ReifnirBot

## About
Source code for the discord bot Reifnir, made for the Norwegian-English Language Learning Exchange (NELLE).

[Join NELLE discord!](https://discord.gg/2d37xPa)

## Local developement

### Requirements

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)
-   [Visual Studio](https://visualstudio.microsoft.com/) (or any other preferred editor + dotnet command line tool)
-   [PostgreSQL 15+](https://www.postgresql.org/)

### Secrets

Configure the bot token and connection string in user secrets:

```
"Nellebot:BotToken": "TOKEN_GOES_HERE"​
"Nellebot:ConnectionString": "CONN_STRING_GOES_HERE"​
```

or as environment variables:

```
Nellebot__BotToken=TOKEN_GOES_HERE
Nellebot__ConnectionString=CONN_STRING_GOES_HERE
```

#### Connection string format

`Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_;`

### Run from Visual Studio or VS Code with C# Dev Kit extension

Set `Nellebot` as the startup project and run the project using `Nellebot` profile.

### Run from dotnet command line tool

`dotnet run --project Nellebot`

### Run as a Docker container in Visual Studio using Fast Mode

#### Nellebot project only

Set `Nellebot` as the startup project and run the project using `Docker` profile.

#### Nellebot and PostgreSQL

Set `docker-vs` as the startup project and run the project using `Docker Compose` profile.

Optionally, use `Compose \W PgAdmin` profile to include a PgAdmin container.

### Run as a Docker container from the command line

#### Nellebot project only

`docker build -t kattbot -f docker/Dockerfile .`

`docker run -d --name kattbot kattbot`

#### Nellebot and PostgreSQL

`docker-compose -f docker/docker-compose.yml up`

Optionally, pass the `--profile tools` flag to include a PgAdmin container.

## Credits

* [DSharp+](https://github.com/DSharpPlus/DSharpPlus) .net discord wrapper
* [Scriban](https://github.com/scriban/scriban) used for generating text and html templates
* [MediatR](https://github.com/jbogard/MediatR) used in CQRS pattern implementation
* [Universitetet i Bergen og Språkrådet](https://www.uib.no) for the Ordbok API
* [Npgsql](https://github.com/npgsql/npgsql) .net data provider for PostgreSQL
