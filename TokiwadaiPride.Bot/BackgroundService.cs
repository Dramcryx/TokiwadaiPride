using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TokiwadaiPride.Bot;
public class BackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundService> _logger;

    public BackgroundService(IServiceProvider serviceProvider, ILogger<BackgroundService> logger)
    {
        _serviceProvider = serviceProvider
            ?? throw new ArgumentException("Service provider for background service is null");
        _logger = logger ?? throw new ArgumentException("Logger for background service is null");
        _logger.LogInformation("Created background service!");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Executing background service");
        // Make sure we receive updates until Cancellation Requested,
        // no matter what errors our ReceiveAsync get
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create new IServiceScope on each iteration.
                // This way we can leverage benefits of Scoped TReceiverService
                // and typed HttpClient - we'll grab "fresh" instance each time
                using var scope = _serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<ReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }
            // Update Handler only captures exception inside update polling loop
            // We'll catch all other exceptions here
            // see: https://github.com/TelegramBots/Telegram.Bot/issues/1106
            catch (Exception ex)
            {
                _logger.LogError($"Polling failed with exception: {ex.Message}", ex);

                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}