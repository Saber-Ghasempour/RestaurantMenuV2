using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantMenu.Notifications.Application;
using RestaurantMenu.Notifications.Infrastructure;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace RestaurantMenu.Notifications.IntegrationTests;

public sealed class CommittedEventDeliveryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq =
        new RabbitMqBuilder("rabbitmq:4.3.5-alpine").Build();

    public Task InitializeAsync() => Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
    public Task DisposeAsync() => Task.WhenAll(_postgres.DisposeAsync().AsTask(),
        _rabbitMq.DisposeAsync().AsTask());

    [Fact]
    public async Task CommittedEventsShouldFlowThroughBrokerAndRecoverAfterOutage()
    {
        var messaging = new MessagingOptions(_rabbitMq.GetConnectionString(),
            $"restaurant-menu.notifications.tests.{Guid.NewGuid():N}", 10,
            TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100), 10,
            TimeSpan.FromSeconds(5));
        var notificationOptions = new NotificationMessagingOptions(
            $"restaurant-menu.notifications.tests.{Guid.NewGuid():N}",
            TimeSpan.FromMilliseconds(100));
        var recorder = new RecordingNotifier();
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        registrations.AddSingleton(messaging);
        registrations.AddSingleton(notificationOptions);
        registrations.AddSingleton<TimeProvider>(TimeProvider.System);
        registrations.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        registrations.AddSingleton<IIntegrationEventPublisher,
            RabbitMqIntegrationEventPublisher>();
        registrations.AddSingleton<IRealtimeNotifier>(recorder);
        registrations.AddScoped<InboxProcessor>();
        registrations.AddScoped<OutboxDispatcher>();
        registrations.AddScoped<IIntegrationEventConsumer,
            OrderPlacedNotificationConsumer>();
        registrations.AddScoped<IIntegrationEventConsumer,
            OrderStatusChangedNotificationConsumer>();
        await using var services = registrations.BuildServiceProvider();
        await using (var scope = services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<OrderingDbContext>()
                .Database.MigrateAsync();
        var worker = new RabbitMqNotificationConsumerWorker(
            services.GetRequiredService<IRabbitMqConnection>(),
            services.GetRequiredService<IServiceScopeFactory>(), messaging,
            notificationOptions, NullLogger<RabbitMqNotificationConsumerWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var unsupported = new IntegrationEventEnvelope(Guid.NewGuid(),
                "ordering.order-placed", 99, Guid.NewGuid(), 1, DateTimeOffset.UtcNow, "{}");
            await services.GetRequiredService<IIntegrationEventPublisher>()
                .PublishAsync(unsupported, CancellationToken.None);
            await using (var deadLetterChannel = await services
                .GetRequiredService<IRabbitMqConnection>()
                .CreateChannelAsync(false, CancellationToken.None))
                await WaitUntilAsync(async () => await deadLetterChannel.BasicGetAsync(
                    $"{notificationOptions.QueueName}.dead-letter", autoAck: true) is not null);

            Order order;
            IntegrationEventEnvelope placedEnvelope;
            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
                order = CreateOrder();
                context.Orders.Add(order);
                Assert.Empty(recorder.Staff);
                await context.SaveChangesAsync();
                Assert.Empty(recorder.Staff);
                placedEnvelope = (await context.OutboxMessages.SingleAsync()).ToEnvelope();
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>()
                    .DispatchBatchAsync(CancellationToken.None);
            }
            await WaitUntilAsync(() => recorder.Staff.Count == 1);
            Assert.Empty(recorder.Guests);
            await services.GetRequiredService<IIntegrationEventPublisher>()
                .PublishAsync(placedEnvelope, CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Assert.Single(recorder.Staff);

            var stopped = await _rabbitMq.ExecAsync(["rabbitmqctl", "stop_app"],
                CancellationToken.None);
            Assert.Equal(0, stopped.ExitCode);
            var started = await _rabbitMq.ExecAsync(["rabbitmqctl", "start_app"],
                CancellationToken.None);
            Assert.Equal(0, started.ExitCode);
            await WaitUntilAsync(async () => await services
                .GetRequiredService<IRabbitMqConnection>().ProbeAsync(CancellationToken.None));

            await using (var scope = services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
                var persisted = await context.Orders.Include(value => value.StatusHistory)
                    .SingleAsync(value => value.Id == order.Id);
                persisted.Accept("test", DateTimeOffset.UtcNow);
                await context.SaveChangesAsync();
                await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>()
                    .DispatchBatchAsync(CancellationToken.None);
            }
            await WaitUntilAsync(() => recorder.Guests.Count == 1 && recorder.Staff.Count == 2);
            Assert.Equal("Accepted", Assert.Single(recorder.Guests).Status);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(timeout.Token);
        }
    }

    private static Order CreateOrder() => Order.Create(OrderId.New(),
        $"M-{Guid.NewGuid():N}"[..20], Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "Table", Guid.NewGuid(), null,
        [new OrderLineSnapshot(Guid.NewGuid(), null, "Tea", null, 2m, "EUR", 1, null)],
        DateTimeOffset.UtcNow).Value;

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!condition()) await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!await condition())
            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
    }

    private sealed class RecordingNotifier : IRealtimeNotifier
    {
        public ConcurrentQueue<OrderRealtimeNotification> Guests { get; } = new();
        public ConcurrentQueue<(Guid RestaurantId, Guid BranchId,
            OrderRealtimeNotification Notification)> Staff { get; } = new();
        public Task NotifyGuestOrderAsync(OrderRealtimeNotification notification,
            CancellationToken cancellationToken)
        { Guests.Enqueue(notification); return Task.CompletedTask; }
        public Task NotifyStaffBranchAsync(Guid restaurantId, Guid branchId,
            OrderRealtimeNotification notification, CancellationToken cancellationToken)
        { Staff.Enqueue((restaurantId, branchId, notification)); return Task.CompletedTask; }
    }
}
