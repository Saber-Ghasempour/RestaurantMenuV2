using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
using RestaurantMenu.Feedback.Infrastructure.Database;
namespace RestaurantMenu.Feedback.Infrastructure;
public sealed class FeedbackRepository(FeedbackDbContext db) : IFeedbackEntryRepository
{
    public void Add(FeedbackEntry entry) => db.FeedbackEntries.Add(entry);
    public Task<FeedbackEntry?> GetAsync(Guid restaurantId, FeedbackEntryId id, CancellationToken cancellationToken) =>
        db.FeedbackEntries.SingleOrDefaultAsync(x => x.RestaurantId == restaurantId && x.Id == id, cancellationToken);
}
public sealed class FeedbackReadService(FeedbackDbContext db) : IFeedbackReadService
{
    public async Task<IReadOnlyList<FeedbackItem>> ListAsync(Guid restaurantId, Guid? branchId, bool includeHidden, int pageNumber, int pageSize, CancellationToken cancellationToken)
    { var query = db.FeedbackEntries.AsNoTracking().Where(x => x.RestaurantId == restaurantId && (!branchId.HasValue || x.BranchId == branchId) && (includeHidden || !x.IsHidden));
      var entries = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
        .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
      return entries.Select(x => new FeedbackItem(x.Id.Value, x.RestaurantId, x.BranchId,
        x.OrderId, x.OrderLineId, x.Rating, x.Sentiment.ToString(), x.Comment,
        x.CreatedAtUtc, x.IsHidden, x.HiddenAtUtc, x.Version)).ToArray(); }
    public async Task<FeedbackSummary> SummarizeAsync(Guid restaurantId, Guid? branchId, CancellationToken cancellationToken)
    { var ratings = await db.FeedbackEntries.AsNoTracking().Where(x => x.RestaurantId == restaurantId && (!branchId.HasValue || x.BranchId == branchId) && !x.IsHidden).Select(x => (int)x.Rating).ToArrayAsync(cancellationToken);
      return new(ratings.Length, ratings.Length == 0 ? 0 : ratings.Average(), ratings.Count(x=>x==1), ratings.Count(x=>x==2), ratings.Count(x=>x==3), ratings.Count(x=>x==4), ratings.Count(x=>x==5)); }
}
