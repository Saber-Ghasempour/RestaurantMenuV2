using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Feedback.Domain.FeedbackEntries;

public readonly record struct FeedbackEntryId(Guid Value);

public enum FeedbackSentiment { Negative = 1, Neutral = 2, Positive = 3 }

public sealed class FeedbackEntry : AggregateRoot<FeedbackEntryId>
{
    public const int MaxCommentLength = 2000;
    private FeedbackEntry() : base(default) { }
    private FeedbackEntry(FeedbackEntryId id, Guid restaurantId, Guid branchId, Guid orderId,
        Guid? orderLineId, Guid diningSessionId, short rating, string? comment, DateTimeOffset createdAtUtc)
        : base(id)
    { RestaurantId = restaurantId; BranchId = branchId; OrderId = orderId; OrderLineId = orderLineId;
      DiningSessionId = diningSessionId; Rating = rating; Sentiment = ToSentiment(rating);
      Comment = comment; CreatedAtUtc = createdAtUtc; }

    public Guid RestaurantId { get; }
    public Guid BranchId { get; }
    public Guid OrderId { get; }
    public Guid? OrderLineId { get; }
    public Guid DiningSessionId { get; }
    public short Rating { get; }
    public FeedbackSentiment Sentiment { get; }
    public string? Comment { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public bool IsHidden { get; private set; }
    public DateTimeOffset? HiddenAtUtc { get; private set; }
    public long Version { get; private set; } = 1;

    public static Result<FeedbackEntry> Create(FeedbackEntryId id, Guid restaurantId, Guid branchId,
        Guid orderId, Guid? orderLineId, Guid diningSessionId, int rating, string? comment,
        DateTimeOffset createdAtUtc)
    {
        if (id.Value == Guid.Empty || restaurantId == Guid.Empty || branchId == Guid.Empty ||
            orderId == Guid.Empty || diningSessionId == Guid.Empty || orderLineId == Guid.Empty)
            return Result.Failure<FeedbackEntry>(FeedbackErrors.InvalidScope);
        if (rating is < 1 or > 5) return Result.Failure<FeedbackEntry>(FeedbackErrors.InvalidRating);
        comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (comment?.Length > MaxCommentLength)
            return Result.Failure<FeedbackEntry>(FeedbackErrors.CommentTooLong);
        return Result.Success(new FeedbackEntry(id, restaurantId, branchId, orderId, orderLineId,
            diningSessionId, (short)rating, comment, createdAtUtc));
    }

    public void Hide(DateTimeOffset now) { if (IsHidden) return; IsHidden = true; HiddenAtUtc = now; Version++; }
    public void Restore() { if (!IsHidden) return; IsHidden = false; HiddenAtUtc = null; Version++; }
    private static FeedbackSentiment ToSentiment(int rating) => rating <= 2 ? FeedbackSentiment.Negative
        : rating == 3 ? FeedbackSentiment.Neutral : FeedbackSentiment.Positive;
}
