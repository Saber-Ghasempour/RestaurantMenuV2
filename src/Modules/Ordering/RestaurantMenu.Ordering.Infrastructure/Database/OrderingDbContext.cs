using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IOrderingUnitOfWork
{
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = CaptureIntegrationEvents();
        try
        {
            var changes = await base.SaveChangesAsync(cancellationToken);
            foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
            return changes;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "ux_dining_sessions_token_hash" })
        { throw new DiningSessionTokenHashAlreadyExistsException("Dining-session token hash already exists.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "ux_idempotency_records_scope_key" })
        { throw new IdempotencyKeyAlreadyExistsException("Idempotency key already exists in this scope.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "ux_outbox_aggregate_version" })
        { throw new ConcurrencyException("A concurrent Ordering database update was detected.", exception); }
        catch (DbUpdateConcurrencyException exception)
        { throw new ConcurrencyException("A concurrent Ordering database update was detected.", exception); }
    }

    private Order[] CaptureIntegrationEvents()
    {
        var aggregates = ChangeTracker.Entries<Order>()
            .Select(entry => entry.Entity)
            .Where(order => order.DomainEvents.Count > 0)
            .ToArray();
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var envelope = OrderingIntegrationEventMapper.Map(domainEvent);
                if (envelope is not null && !OutboxMessages.Local.Any(message =>
                    message.AggregateId == envelope.AggregateId &&
                    message.AggregateVersion == envelope.AggregateVersion))
                    OutboxMessages.Add(new OutboxMessage(envelope));
            }
        }
        return aggregates;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
