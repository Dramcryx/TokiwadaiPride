# TokiwadaiPride

## A Telegram bot for logging personal expenses

## About
The bot consists of three services:
1. `TokiwadaiPride.Bot` - the bot itself. Handles incoming messages, validates arguments and passes them to a database service.
2. `TokiwadaiPride.Database` - the database service. Uses SQLite to store the data locally.
3. `TokiwadaiPride.Web` - the service for Web UI of expense data.

Other parts:
- `TokiwadaiPride.Contract` - contracts between bot and database services.
- `TokiwadaiPride.Database.Client` - a client wrapper over raw HTTP for the database service.
- `TokiwadaiPride.Redis` - Redis wrapper for use in bot and web services.

### Requirements

1. `dotnet` 8 SDK;
2. A bot API token obtained via `@BotFather` bot;
3. `docker` installed;
4. `node` installed.

### How to run
1. Set bot token as a value to `BotToken` property in `TokiwadaiPride.Bot/appSettings.json`;
2. Run `run.sh` script.
