using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Application.Feedback;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
namespace RestaurantMenu.Feedback.Application.UnitTests;
public sealed class CreateFeedbackHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026,9,10,12,0,0,TimeSpan.Zero);
    [Fact] public async Task RejectsUnownedOrIncompleteOrder() { var sut=Create(null,out _); var r=await sut.Handle(new("token",Guid.NewGuid(),null,5,null),default); Assert.Equal(FeedbackErrors.NotEligible,r.Error); }
    [Fact] public async Task RejectsExpiredWindow() { var e=Eligible(Now.AddDays(-8)); var sut=Create(e,out _); var r=await sut.Handle(new("token",e.OrderId,null,5,null),default); Assert.Equal(FeedbackErrors.NotEligible,r.Error); }
    [Fact] public async Task RejectsLineOutsideOwnedOrder() { var e=Eligible(Now); var sut=Create(e,out _); var r=await sut.Handle(new("token",e.OrderId,Guid.NewGuid(),5,null),default); Assert.Equal(FeedbackErrors.NotEligible,r.Error); }
    [Fact] public async Task PersistsEligibleFeedback() { var e=Eligible(Now); var sut=Create(e,out var repo); var line=e.OrderLineIds.Single(); var r=await sut.Handle(new("token",e.OrderId,line,4," great "),default); Assert.True(r.IsSuccess); Assert.Equal("great",repo.Entry!.Comment); Assert.Equal(e.RestaurantId,repo.Entry.RestaurantId); }
    private static CreateFeedbackCommandHandler Create(FeedbackEligibility? e,out Repo repo) { repo=new(); return new(new Eligibility(e),repo,repo,new FeedbackOptions(TimeSpan.FromDays(7)),new Clock()); }
    private static FeedbackEligibility Eligible(DateTimeOffset completed) => new(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),completed,new HashSet<Guid>{Guid.NewGuid()});
    private sealed class Eligibility(FeedbackEligibility? value):IFeedbackEligibilityProvider { public Task<FeedbackEligibility?> GetAsync(string token,Guid orderId,CancellationToken cancellationToken)=>Task.FromResult(value); }
    private sealed class Repo:IFeedbackEntryRepository,IFeedbackUnitOfWork { public FeedbackEntry? Entry; public void Add(FeedbackEntry entry)=>Entry=entry; public Task<FeedbackEntry?> GetAsync(Guid r,FeedbackEntryId id,CancellationToken c)=>Task.FromResult<FeedbackEntry?>(null); public Task<int> SaveChangesAsync(CancellationToken c=default)=>Task.FromResult(1); }
    private sealed class Clock:TimeProvider { public override DateTimeOffset GetUtcNow()=>Now; }
}
