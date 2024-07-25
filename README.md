# TokiwadaiPride

## A Telegram bot for logging personal expenses

## About
The bot consists of two services:
1. `TokiwadaiPride.Bot` - the bot itself. Handles incoming messages, validates arguments and passes them to a database service.
2. `TokiwadaiPride.Database` - the database service. Uses SQLite to store the data locally.

Other parts:
- `TokiwadaiPride.Contract` - contracts between bot and database services.
- `TokiwadaiPride.Database.Client` - a client wrapper over raw HTTP for the database service.

### Requirements

1. `dotnet` 8 SDK;
2. A bot API token obtained via `@BotFather` bot.

### How to run
0. You will require two consoles to start both services individually;
1. There is a silly thing that has not been fixed yet - configuration JSON is not read properly, so hardcode token and database URL for now in `TokiwadaiPride.Contract/Types/BotConfiguration.cs`:
```c#
public class BotConfiguration
{
    public static readonly string Configuration = "BotConfiguration";
    
    public string BotToken { get; set; } = "<your token>";
    public string DatabaseClientUrl { get; set; } = "http://localhost:5051";
}
```
2. Start database service from repository root typing in next command:

`dotnet run --project TokiwadaiPride.Database/TokiwadaiPride.Database.csproj`;

3. Start bot from repository root typing in next command:

`dotnet run --project TokiwadaiPride.Bot/TokiwadaiPride.Bot.csproj`;

4. Test a bot sending any command.