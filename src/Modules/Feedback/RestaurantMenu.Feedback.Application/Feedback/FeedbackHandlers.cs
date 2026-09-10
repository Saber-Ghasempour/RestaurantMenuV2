using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Feedback.Application.Feedback;

public sealed record FeedbackOptions(TimeSpan SubmissionWindow);
public sealed record CreateFeedbackCommand(string DiningSessionToken, Guid OrderId, Guid? OrderLineId,
    int Rating, string? Comment) : ICommand<Result<FeedbackItem>>;
public sealed class CreateFeedbackCommandHandler(IFeedbackEligibilityProvider eligibility,
    IFeedbackEntryRepository repository, IFeedbackUnitOfWork unitOfWork,
    FeedbackOptions options, TimeProvider timeProvider) : ICommandHandler<CreateFeedbackCommand, Result<FeedbackItem>>
{
    public async Task<Result<FeedbackItem>> Handle(CreateFeedbackCommand command, CancellationToken cancellationToken)
    {
        var scope = await eligibility.GetAsync(command.DiningSessionToken, command.OrderId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (scope is null || now > scope.CompletedAtUtc + options.SubmissionWindow ||
            command.OrderLineId is Guid lineId && !scope.OrderLineIds.Contains(lineId))
            return Result.Failure<FeedbackItem>(FeedbackErrors.NotEligible);
        var created = FeedbackEntry.Create(new(Guid.NewGuid()), scope.RestaurantId, scope.BranchId,
            scope.OrderId, command.OrderLineId, scope.DiningSessionId, command.Rating, command.Comment, now);
        if (created.IsFailure) return Result.Failure<FeedbackItem>(created.Error);
        repository.Add(created.Value);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DuplicateFeedbackException) { return Result.Failure<FeedbackItem>(FeedbackErrors.Duplicate); }
        return Result.Success(ToItem(created.Value));
    }
    internal static FeedbackItem ToItem(FeedbackEntry value) => new(value.Id.Value, value.RestaurantId,
        value.BranchId, value.OrderId, value.OrderLineId, value.Rating, value.Sentiment.ToString(),
        value.Comment, value.CreatedAtUtc, value.IsHidden, value.HiddenAtUtc, value.Version);
}
public sealed record ModerateFeedbackCommand(Guid RestaurantId, Guid FeedbackId, long ExpectedVersion,
    bool Hide) : ICommand<Result<FeedbackItem>>;
public sealed class ModerateFeedbackCommandHandler(IFeedbackEntryRepository repository,
    IFeedbackUnitOfWork unitOfWork, TimeProvider timeProvider) : ICommandHandler<ModerateFeedbackCommand, Result<FeedbackItem>>
{
    public async Task<Result<FeedbackItem>> Handle(ModerateFeedbackCommand command, CancellationToken cancellationToken)
    {
        var entry = await repository.GetAsync(command.RestaurantId, new(command.FeedbackId), cancellationToken);
        if (entry is null) return Result.Failure<FeedbackItem>(FeedbackErrors.NotFound);
        if (entry.Version != command.ExpectedVersion) return Result.Failure<FeedbackItem>(FeedbackErrors.Concurrency);
        if (command.Hide) entry.Hide(timeProvider.GetUtcNow()); else entry.Restore();
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (FeedbackConcurrencyException) { return Result.Failure<FeedbackItem>(FeedbackErrors.Concurrency); }
        return Result.Success(CreateFeedbackCommandHandler.ToItem(entry));
    }
}
public sealed record ListFeedbackQuery(Guid RestaurantId, Guid? BranchId, bool IncludeHidden,
    int PageNumber, int PageSize) : IQuery<Result<IReadOnlyList<FeedbackItem>>>;
public sealed class ListFeedbackQueryHandler(IFeedbackReadService reads) : IQueryHandler<ListFeedbackQuery, Result<IReadOnlyList<FeedbackItem>>>
{ public async Task<Result<IReadOnlyList<FeedbackItem>>> Handle(ListFeedbackQuery query, CancellationToken cancellationToken) =>
    Result.Success(await reads.ListAsync(query.RestaurantId, query.BranchId, query.IncludeHidden,
        Math.Max(1, query.PageNumber), Math.Clamp(query.PageSize, 1, 100), cancellationToken)); }
public sealed record GetFeedbackSummaryQuery(Guid RestaurantId, Guid? BranchId) : IQuery<Result<FeedbackSummary>>;
public sealed class GetFeedbackSummaryQueryHandler(IFeedbackReadService reads) : IQueryHandler<GetFeedbackSummaryQuery, Result<FeedbackSummary>>
{ public async Task<Result<FeedbackSummary>> Handle(GetFeedbackSummaryQuery query, CancellationToken cancellationToken) =>
    Result.Success(await reads.SummarizeAsync(query.RestaurantId, query.BranchId, cancellationToken)); }
