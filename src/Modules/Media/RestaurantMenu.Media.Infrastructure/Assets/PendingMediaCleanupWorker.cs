using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Domain.Assets;

namespace RestaurantMenu.Media.Infrastructure.Assets;

public sealed class PendingMediaCleanupWorker(IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider, ILogger<PendingMediaCleanupWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan PendingRetention = TimeSpan.FromHours(24);
    private static readonly Action<ILogger, Exception?> LogCleanupFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, "MediaCleanupFailed"),
            "Media pending-object cleanup failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await CleanupAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { LogCleanupFailure(logger, exception); }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var reads = scope.ServiceProvider.GetRequiredService<IMediaAssetReadService>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IMediaUnitOfWork>();
        var now = timeProvider.GetUtcNow();
        var expired = await reads.GetExpiredPendingAsync(now.Subtract(PendingRetention), cancellationToken);
        foreach (var asset in expired)
        {
            if (asset.Status == MediaAssetStatus.Pending) asset.Reject(now);
        }
        if (expired.Count > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var asset in expired) await storage.DeleteAsync(asset.StorageKey, cancellationToken);
    }
}
