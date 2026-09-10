using Microsoft.EntityFrameworkCore;
using Npgsql;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
namespace RestaurantMenu.Feedback.Infrastructure.Database;
public sealed class FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : DbContext(options), IFeedbackUnitOfWork
{
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { modelBuilder.HasDefaultSchema("feedback"); modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeedbackDbContext).Assembly); }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_feedback_session_order_line" or "ux_feedback_session_order" })
        { throw new DuplicateFeedbackException("Feedback already exists.", ex); }
        catch (DbUpdateConcurrencyException ex) { throw new FeedbackConcurrencyException("Concurrent feedback update.", ex); }
    }
}
