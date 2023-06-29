using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TokiwadaiPride.Bot.Database;

namespace TokiwadaiPride.Bot;
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<BotConfiguration>(
                    context.Configuration.GetSection(BotConfiguration.Configuration));
                services.AddHttpClient("TokiwadaiPride.Bot.Client")
                        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                        {
                            BotConfiguration? botConfig = sp.GetService<IOptions<BotConfiguration>>()?.Value;
                            if (botConfig == null)
                            {
                                throw new ArgumentException("No bot configuration");
                            }
                            TelegramBotClientOptions options = new(botConfig.BotToken);
                            return new TelegramBotClient(options, httpClient);
                        });

                services.AddScoped<DatabaseClient>();
                services.AddScoped<ExpenseHandler>();
                services.AddScoped<ReceiverService>();
                services.AddHostedService<BackgroundService>();
            })
            .Build();

        await host.RunAsync();
    }
}
