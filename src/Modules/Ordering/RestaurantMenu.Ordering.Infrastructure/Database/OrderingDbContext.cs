using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Infrastructure.Database;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options)
    : DbContext(options), IOrderingUnitOfWork
{
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "ux_dining_sessions_token_hash" })
        { throw new DiningSessionTokenHashAlreadyExistsException("Dining-session token hash already exists.", exception); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation,
              ConstraintName: "ux_idempotency_records_scope_key" })
        { throw new IdempotencyKeyAlreadyExistsException("Idempotency key already exists in this scope.", exception); }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
