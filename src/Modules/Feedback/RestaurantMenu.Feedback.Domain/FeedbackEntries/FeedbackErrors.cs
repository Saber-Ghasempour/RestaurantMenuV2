using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Feedback.Domain.FeedbackEntries;
public static class FeedbackErrors
{
    public static readonly ErrorDetail InvalidScope = ErrorDetail.Validation("Feedback.InvalidScope", "The feedback scope is invalid.");
    public static readonly ErrorDetail InvalidRating = ErrorDetail.Validation("Feedback.InvalidRating", "Rating must be between 1 and 5.");
    public static readonly ErrorDetail CommentTooLong = ErrorDetail.Validation("Feedback.CommentTooLong", $"Comment cannot exceed {FeedbackEntry.MaxCommentLength} characters.");
    public static readonly ErrorDetail NotEligible = ErrorDetail.NotFound("Feedback.NotEligible", "The order is not eligible for feedback.");
    public static readonly ErrorDetail Duplicate = ErrorDetail.Conflict("Feedback.Duplicate", "Feedback was already submitted for this order line and dining session.");
    public static readonly ErrorDetail NotFound = ErrorDetail.NotFound("Feedback.NotFound", "Feedback was not found.");
    public static readonly ErrorDetail Concurrency = ErrorDetail.Conflict("Feedback.Concurrency", "Feedback was changed by another request.");
}
