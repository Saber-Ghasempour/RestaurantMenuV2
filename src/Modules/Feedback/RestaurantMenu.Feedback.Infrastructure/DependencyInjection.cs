using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Feedback.Application.Abstractions;
using RestaurantMenu.Feedback.Application.Feedback;
using RestaurantMenu.Feedback.Infrastructure.Database;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Feedback.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddFeedbackInfrastructure(this IServiceCollection services, string connectionString, TimeSpan window)
    {
        if (window <= TimeSpan.Zero || window > TimeSpan.FromDays(30)) throw new ArgumentOutOfRangeException(nameof(window));
        services.AddDbContext<FeedbackDbContext>(o => o.UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__ef_migrations_history", "feedback")));
        services.AddScoped<IFeedbackEntryRepository, FeedbackRepository>(); services.AddScoped<IFeedbackReadService, FeedbackReadService>();
        services.AddScoped<IFeedbackUnitOfWork>(sp => sp.GetRequiredService<FeedbackDbContext>()); services.AddSingleton(new FeedbackOptions(window));
        services.AddScoped<ICommandHandler<CreateFeedbackCommand, Result<FeedbackItem>>, CreateFeedbackCommandHandler>();
        services.AddScoped<ICommandHandler<ModerateFeedbackCommand, Result<FeedbackItem>>, ModerateFeedbackCommandHandler>();
        services.AddScoped<IQueryHandler<ListFeedbackQuery, Result<IReadOnlyList<FeedbackItem>>>, ListFeedbackQueryHandler>();
        services.AddScoped<IQueryHandler<GetFeedbackSummaryQuery, Result<FeedbackSummary>>, GetFeedbackSummaryQueryHandler>();
        return services;
    }
}
