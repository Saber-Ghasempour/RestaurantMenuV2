using RestaurantMenu.Feedback.Domain.FeedbackEntries;
namespace RestaurantMenu.Feedback.Domain.UnitTests;
public sealed class FeedbackEntryTests
{
    [Theory] [InlineData(0)] [InlineData(6)] public void CreateRejectsRatingOutsideRange(int rating) =>
        Assert.Equal(FeedbackErrors.InvalidRating, Create(rating, null).Error);
    [Fact] public void CreateRejectsLongComment() => Assert.Equal(FeedbackErrors.CommentTooLong,
        Create(5, new string('x', FeedbackEntry.MaxCommentLength + 1)).Error);
    [Theory] [InlineData(1, FeedbackSentiment.Negative)] [InlineData(3, FeedbackSentiment.Neutral)]
    [InlineData(5, FeedbackSentiment.Positive)] public void CreateDerivesSentiment(int rating, FeedbackSentiment expected) =>
        Assert.Equal(expected, Create(rating, " useful ").Value.Sentiment);
    [Fact] public void HideAndRestoreTrackModerationAndVersion()
    { var entry=Create(4,null).Value; var now=DateTimeOffset.UtcNow; entry.Hide(now); Assert.True(entry.IsHidden); Assert.Equal(now,entry.HiddenAtUtc); Assert.Equal(2,entry.Version); entry.Restore(); Assert.False(entry.IsHidden); Assert.Null(entry.HiddenAtUtc); Assert.Equal(3,entry.Version); }
    private static RestaurantMenu.SharedKernel.Results.Result<FeedbackEntry> Create(int rating,string? comment) =>
        FeedbackEntry.Create(new(Guid.NewGuid()),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),null,Guid.NewGuid(),rating,comment,DateTimeOffset.UtcNow);
}
