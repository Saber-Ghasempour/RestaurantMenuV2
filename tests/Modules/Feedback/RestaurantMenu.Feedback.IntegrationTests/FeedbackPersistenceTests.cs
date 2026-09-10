using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Domain.FeedbackEntries;
using RestaurantMenu.Feedback.Infrastructure;
using RestaurantMenu.Feedback.Infrastructure.Database;
using Testcontainers.PostgreSql;
namespace RestaurantMenu.Feedback.IntegrationTests;
public sealed class FeedbackPersistenceTests:IAsyncLifetime
{
    private readonly PostgreSqlContainer db=new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    public Task InitializeAsync()=>db.StartAsync(); public Task DisposeAsync()=>db.DisposeAsync().AsTask();
    [Fact] public async Task MigrationEnforcesDuplicateAndSummaryExcludesHidden()
    { var o=Options(); var rid=Guid.NewGuid();var bid=Guid.NewGuid();var oid=Guid.NewGuid();var sid=Guid.NewGuid();
      await using(var setup=new FeedbackDbContext(o)){await setup.Database.MigrateAsync();setup.Add(Make(rid,bid,oid,sid,null,1));setup.Add(Make(rid,bid,Guid.NewGuid(),sid,null,5));await setup.SaveChangesAsync();}
      await using(var duplicate=new FeedbackDbContext(o)){duplicate.Add(Make(rid,bid,oid,sid,null,3));await Assert.ThrowsAsync<DuplicateFeedbackException>(()=>duplicate.SaveChangesAsync());}
      await using(var change=new FeedbackDbContext(o)){var first=await change.FeedbackEntries.OrderBy(x=>x.Rating).FirstAsync();first.Hide(DateTimeOffset.UtcNow);await change.SaveChangesAsync();}
      await using var verify=new FeedbackDbContext(o);var reads=new FeedbackReadService(verify);var summary=await reads.SummarizeAsync(rid,null,default);Assert.Equal(1,summary.TotalCount);Assert.Equal(5,summary.AverageRating);var list=await reads.ListAsync(rid,null,true,1,20,default);Assert.Equal(2,list.Count); }
    private DbContextOptions<FeedbackDbContext> Options()=>new DbContextOptionsBuilder<FeedbackDbContext>().UseNpgsql(db.GetConnectionString()).Options;
    private static FeedbackEntry Make(Guid r,Guid b,Guid o,Guid s,Guid? l,int rating)=>FeedbackEntry.Create(new(Guid.NewGuid()),r,b,o,l,s,rating,null,DateTimeOffset.UtcNow).Value;
}
