namespace RestaurantMenu.Feedback.Application.Abstractions;
public sealed record FeedbackItem(Guid Id, Guid RestaurantId, Guid BranchId, Guid OrderId,
    Guid? OrderLineId, short Rating, string Sentiment, string? Comment, DateTimeOffset CreatedAtUtc,
    bool IsHidden, DateTimeOffset? HiddenAtUtc, long Version);
public sealed record FeedbackSummary(int TotalCount, double AverageRating,
    int OneStar, int TwoStar, int ThreeStar, int FourStar, int FiveStar);
public interface IFeedbackReadService
{
    Task<IReadOnlyList<FeedbackItem>> ListAsync(Guid restaurantId, Guid? branchId, bool includeHidden,
        int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<FeedbackSummary> SummarizeAsync(Guid restaurantId, Guid? branchId, CancellationToken cancellationToken);
}
