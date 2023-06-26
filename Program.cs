using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TokiwadaiPride;
using Microsoft.Extensions.Options;

public class Program
{
    private static CancellationToken _globalCancel = new CancellationToken();

    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<BotConfiguration>(
                    context.Configuration.GetSection(BotConfiguration.Configuration));
                services.AddHttpClient("telegram_bot_client")
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
                
                services.AddScoped<ExpenseHandler>();
                services.AddScoped<ReceiverService>();
                services.AddHostedService<TokiwadaiPride.BackgroundService>();
            })
            .Build();
        await host.RunAsync();
    }

    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string BotToken { get; set; } = "";
    }
}
