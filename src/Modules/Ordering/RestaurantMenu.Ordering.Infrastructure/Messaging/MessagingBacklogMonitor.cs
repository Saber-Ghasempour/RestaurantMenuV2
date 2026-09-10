using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class MessagingBacklogMonitor(
    OrderingDbContext dbContext,
    TimeProvider timeProvider)
{
    public async Task CollectAsync(CancellationToken cancellationToken)
    {
        var oldest = await dbContext.OutboxMessages
            .Where(value => value.ProcessedAtUtc == null &&
                value.DeadLetteredAtUtc == null)
            .Select(value => (DateTimeOffset?)value.OccurredAtUtc)
            .MinAsync(cancellationToken);
        var outboxDeadLetters = await dbContext.OutboxMessages
            .CountAsync(value => value.DeadLetteredAtUtc != null, cancellationToken);
        var inboxDeadLetters = await dbContext.InboxMessages
            .CountAsync(value => value.DeadLetteredAtUtc != null, cancellationToken);
        var age = oldest is null
            ? 0
            : (long)Math.Max(0, (timeProvider.GetUtcNow() - oldest.Value).TotalSeconds);
        MessagingTelemetry.UpdateBacklog(age, outboxDeadLetters, inboxDeadLetters);
    }
}

internal sealed class MessagingBacklogMonitorWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MessagingBacklogMonitorWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CollectionInterval = TimeSpan.FromSeconds(30);
    private static readonly Action<ILogger, Exception?> LogCollectionFailure =
        LoggerMessage.Define(LogLevel.Warning,
            new EventId(4103, "MessagingMetricsCollectionFailed"),
            "Messaging backlog metric collection failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CollectionInterval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<MessagingBacklogMonitor>()
                    .CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogCollectionFailure(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
