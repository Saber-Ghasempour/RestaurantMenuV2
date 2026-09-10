using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class OutboxPublisherWorker(IServiceScopeFactory scopeFactory,
    MessagingOptions options, ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogPollingFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(4102, "OutboxPollingFailed"),
            "Outbox polling failed; the durable backlog will be retried.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>()
                    .DispatchBatchAsync(stoppingToken);
                if (count > 0) continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            { break; }
            catch (Exception exception)
            { LogPollingFailure(logger, exception); }

            await Task.Delay(options.PollingInterval, stoppingToken);
        }
    }
}
