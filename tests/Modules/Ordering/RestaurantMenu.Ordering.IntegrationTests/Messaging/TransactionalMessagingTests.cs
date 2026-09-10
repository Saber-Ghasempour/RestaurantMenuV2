using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.Messaging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace RestaurantMenu.Ordering.IntegrationTests.Messaging;

public sealed class TransactionalMessagingTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq =
        new RabbitMqBuilder("rabbitmq:4.3.5-alpine").Build();

    public Task InitializeAsync() => Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
    public async Task DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task OrderAndVersionedOutboxEventsShouldCommitAtomicallyAndInOrder()
    {
        var options = Options();
        var order = CreateOrder();
        await using (var context = new OrderingDbContext(options))
        {
            await context.Database.MigrateAsync();
            Assert.False(context.Database.HasPendingModelChanges());
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            order.Accept("cashier", DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        }

        await using var verify = new OrderingDbContext(options);
        var messages = await verify.OutboxMessages.AsNoTracking()
            .OrderBy(value => value.AggregateVersion).ToArrayAsync();
        Assert.Collection(messages,
            value => { Assert.Equal("ordering.order-placed", value.Name); Assert.Equal(1, value.EventVersion); Assert.Equal(1, value.AggregateVersion); },
            value => { Assert.Equal("ordering.order-status-changed", value.Name); Assert.Equal(1, value.EventVersion); Assert.Equal(2, value.AggregateVersion); });
        Assert.All(messages, value => Assert.Contains(order.Id.Value.ToString(), value.Payload, StringComparison.OrdinalIgnoreCase));

        var rolledBack = CreateOrder();
        await using (var context = new OrderingDbContext(options))
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            context.Orders.Add(rolledBack);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.False(await verify.Orders.AnyAsync(value => value.Id == rolledBack.Id));
        Assert.False(await verify.OutboxMessages.AnyAsync(value => value.AggregateId == rolledBack.Id.Value));
    }

    [Fact]
    public async Task DispatcherShouldRetryThenSucceedAndDeadLetterPoisonMessages()
    {
        var options = Options();
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var context = new OrderingDbContext(options);
        await context.Database.MigrateAsync();
        context.Orders.Add(CreateOrder(clock.GetUtcNow()));
        await context.SaveChangesAsync();
        var publisher = new ScriptedPublisher([false, true]);
        var messaging = OptionsFor("amqp://unused", maximumAttempts: 3);
        var dispatcher = new OutboxDispatcher(context, publisher, messaging, clock,
            NullLogger<OutboxDispatcher>.Instance);

        Assert.Equal(1, await dispatcher.DispatchBatchAsync(CancellationToken.None));
        var failed = await context.OutboxMessages.SingleAsync();
        Assert.Equal(1, failed.AttemptCount);
        Assert.Null(failed.ProcessedAtUtc);
        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(1, await dispatcher.DispatchBatchAsync(CancellationToken.None));
        Assert.NotNull(failed.ProcessedAtUtc);

        var poisonOrder = CreateOrder(clock.GetUtcNow());
        context.Orders.Add(poisonOrder);
        await context.SaveChangesAsync();
        var poison = new OutboxDispatcher(context, new ScriptedPublisher([false, false]),
            OptionsFor("amqp://unused", maximumAttempts: 2), clock,
            NullLogger<OutboxDispatcher>.Instance);
        await poison.DispatchBatchAsync(CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(2));
        await poison.DispatchBatchAsync(CancellationToken.None);
        var dead = await context.OutboxMessages.SingleAsync(value =>
            value.AggregateId == poisonOrder.Id.Value);
        Assert.Equal(2, dead.AttemptCount);
        Assert.NotNull(dead.DeadLetteredAtUtc);
    }

    [Fact]
    public async Task InboxShouldApplyEffectOnceAndDeadLetterRepeatedPoisonDelivery()
    {
        var options = Options();
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var context = new OrderingDbContext(options);
        await context.Database.MigrateAsync();
        var order = CreateOrder(clock.GetUtcNow());
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var envelope = Envelope(order, Guid.NewGuid());
        var consumer = new AcceptOrderConsumer(context, order.Id);
        var processor = new InboxProcessor(context, OptionsFor("amqp://unused", 3), clock);

        Assert.Equal(InboxProcessingOutcome.Processed,
            await processor.ProcessAsync(envelope, consumer, CancellationToken.None));
        Assert.Equal(InboxProcessingOutcome.Duplicate,
            await processor.ProcessAsync(envelope, consumer, CancellationToken.None));
        Assert.Equal(1, consumer.Invocations);
        Assert.Equal(OrderStatus.Accepted, order.Status);
        Assert.Equal(2, order.StatusHistory.Count);

        var poisonEnvelope = envelope with { Id = Guid.NewGuid() };
        var poison = new PoisonConsumer();
        Assert.Equal(InboxProcessingOutcome.RetryScheduled,
            await processor.ProcessAsync(poisonEnvelope, poison, CancellationToken.None));
        Assert.Equal(InboxProcessingOutcome.RetryScheduled,
            await processor.ProcessAsync(poisonEnvelope, poison, CancellationToken.None));
        Assert.Equal(InboxProcessingOutcome.DeadLettered,
            await processor.ProcessAsync(poisonEnvelope, poison, CancellationToken.None));
        Assert.Equal(InboxProcessingOutcome.DeadLettered,
            await processor.ProcessAsync(poisonEnvelope, poison, CancellationToken.None));
        Assert.Equal(3, poison.Invocations);
        var receipt = await context.InboxMessages.SingleAsync(value => value.MessageId == poisonEnvelope.Id);
        Assert.Equal(3, receipt.AttemptCount);
        Assert.NotNull(receipt.DeadLetteredAtUtc);
    }

    [Fact]
    public async Task RabbitMqPublisherShouldSendPersistentVersionedEnvelopeWithConfirm()
    {
        var messaging = OptionsFor(_rabbitMq.GetConnectionString(), 3);
        await using var connection = new RabbitMqConnection(messaging);
        await using var channel = await connection.CreateChannelAsync(false, CancellationToken.None);
        await channel.ExchangeDeclareAsync(messaging.ExchangeName, ExchangeType.Topic,
            durable: true, autoDelete: false, arguments: null);
        var declared = await channel.QueueDeclareAsync(string.Empty, durable: false,
            exclusive: true, autoDelete: true, arguments: null);
        await channel.QueueBindAsync(declared.QueueName, messaging.ExchangeName,
            "ordering.order-placed", arguments: null);
        var envelope = Envelope(CreateOrder(), Guid.NewGuid());
        var publisher = new RabbitMqIntegrationEventPublisher(connection, messaging);

        await publisher.PublishAsync(envelope, CancellationToken.None);
        var delivery = await channel.BasicGetAsync(declared.QueueName, autoAck: true);

        Assert.NotNull(delivery);
        Assert.Equal(envelope.Id.ToString("D"), delivery.BasicProperties.MessageId);
        Assert.Equal(envelope.Name, delivery.BasicProperties.Type);
        Assert.Equal(envelope.Payload, Encoding.UTF8.GetString(delivery.Body.Span));
        Assert.Equal(DeliveryModes.Persistent, delivery.BasicProperties.DeliveryMode);
    }

    [Fact]
    public async Task DispatcherShouldRecoverAfterRealBrokerOutage()
    {
        var databaseOptions = Options();
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        await using var context = new OrderingDbContext(databaseOptions);
        await context.Database.MigrateAsync();
        context.Orders.Add(CreateOrder(clock.GetUtcNow()));
        await context.SaveChangesAsync();
        var messaging = OptionsFor(_rabbitMq.GetConnectionString(), 20);
        await using var connection = new RabbitMqConnection(messaging);
        Assert.True(await connection.ProbeAsync(CancellationToken.None));
        var dispatcher = new OutboxDispatcher(context,
            new RabbitMqIntegrationEventPublisher(connection, messaging), messaging, clock,
            NullLogger<OutboxDispatcher>.Instance);

        var stopped = await _rabbitMq.ExecAsync(["rabbitmqctl", "stop_app"],
            CancellationToken.None);
        Assert.Equal(0, stopped.ExitCode);
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            Assert.Equal(1, await dispatcher.DispatchBatchAsync(timeout.Token));
        var message = await context.OutboxMessages.SingleAsync();
        Assert.Equal(1, message.AttemptCount);
        Assert.Null(message.ProcessedAtUtc);

        var started = await _rabbitMq.ExecAsync(["rabbitmqctl", "start_app"],
            CancellationToken.None);
        Assert.Equal(0, started.ExitCode);
        for (var attempt = 0; attempt < 10 && message.ProcessedAtUtc is null; attempt++)
        {
            clock.Advance(TimeSpan.FromMinutes(2));
            await dispatcher.DispatchBatchAsync(CancellationToken.None);
            if (message.ProcessedAtUtc is null)
                await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
        Assert.True(message.ProcessedAtUtc is not null,
            $"Attempts: {message.AttemptCount}; last error: {message.LastError}");
    }

    [Fact]
    public async Task OutboxWorkerShouldStopWhileWaitingForItsNextPoll()
    {
        var databaseOptions = Options();
        await using (var context = new OrderingDbContext(databaseOptions))
            await context.Database.MigrateAsync();
        var messaging = OptionsFor("amqp://unused", 3) with
        { PollingInterval = TimeSpan.FromHours(1) };
        var registrations = new ServiceCollection();
        registrations.AddLogging();
        registrations.AddDbContext<OrderingDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        registrations.AddSingleton(messaging);
        registrations.AddSingleton<TimeProvider>(TimeProvider.System);
        registrations.AddSingleton<IIntegrationEventPublisher>(new ScriptedPublisher([]));
        registrations.AddScoped<OutboxDispatcher>();
        await using var services = registrations.BuildServiceProvider();
        var worker = new OutboxPublisherWorker(services.GetRequiredService<IServiceScopeFactory>(),
            messaging, NullLogger<OutboxPublisherWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StopAsync(timeout.Token);
    }

    private DbContextOptions<OrderingDbContext> Options() =>
        new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;

    private static MessagingOptions OptionsFor(string connectionString, int maximumAttempts) =>
        new(connectionString, $"restaurant-menu.tests.{Guid.NewGuid():N}", 10,
            TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1), maximumAttempts,
            TimeSpan.FromMinutes(1));

    private static IntegrationEventEnvelope Envelope(Order order, Guid id) => new(id,
        "ordering.order-placed", 1, order.Id.Value, order.Version, order.CreatedAtUtc,
        "{\"event\":\"order-placed\"}");

    private static Order CreateOrder(DateTimeOffset? now = null) => Order.Create(OrderId.New(),
        $"M-{Guid.NewGuid():N}"[..20], Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Table",
        Guid.NewGuid(), null,
        [new OrderLineSnapshot(Guid.NewGuid(), null, "Tea", null, 2m, "EUR", 1, null)],
        now ?? DateTimeOffset.UtcNow).Value;

    private sealed class ScriptedPublisher(IEnumerable<bool> outcomes) : IIntegrationEventPublisher
    {
        private readonly Queue<bool> _outcomes = new(outcomes);
        public Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
        {
            if (!_outcomes.Dequeue()) throw new InvalidOperationException("broker unavailable");
            return Task.CompletedTask;
        }
    }

    private sealed class AcceptOrderConsumer(OrderingDbContext context, OrderId orderId)
        : IIntegrationEventConsumer
    {
        public int Invocations { get; private set; }
        public string ConsumerName => "test.accept-order";
        public string EventName => "ordering.order-placed";
        public int EventVersion => 1;
        public async Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
        {
            Invocations++;
            var order = await context.Orders.Include(value => value.StatusHistory)
                .SingleAsync(value => value.Id == orderId, cancellationToken);
            order.Accept("test-consumer", DateTimeOffset.UtcNow);
        }
    }

    private sealed class PoisonConsumer : IIntegrationEventConsumer
    {
        public int Invocations { get; private set; }
        public string ConsumerName => "test.poison";
        public string EventName => "ordering.order-placed";
        public int EventVersion => 1;
        public Task HandleAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
        { Invocations++; throw new InvalidOperationException("poison payload"); }
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
