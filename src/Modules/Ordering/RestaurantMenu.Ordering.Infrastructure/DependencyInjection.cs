using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Application.DiningSessions.StartDiningSession;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.DiningSessions;
using RestaurantMenu.Ordering.Application.Orders.PlaceOrder;
using RestaurantMenu.Ordering.Infrastructure.Orders;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Application.Orders.Queues;
using RestaurantMenu.Ordering.Application.Orders.GuestOrders;
using RestaurantMenu.Ordering.Infrastructure.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services,
        string connectionString, TimeSpan sessionLifetime, MessagingOptions? messagingOptions = null)
    {
        if (sessionLifetime <= TimeSpan.Zero || sessionLifetime > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime), "Dining-session lifetime must be between zero and 24 hours.");
        services.AddDbContext<OrderingDbContext>(options => options.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "ordering")));
        services.AddScoped<IDiningSessionRepository, DiningSessionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IOrderReadService, OrderReadService>();
        services.AddScoped<IOrderingUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());
        services.AddSingleton<IDiningSessionTokenGenerator, CryptographicDiningSessionTokenGenerator>();
        services.AddSingleton(new DiningSessionOptions(sessionLifetime));
        services.AddSingleton(new GuestOrderOptions(TimeSpan.FromMinutes(5)));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICommandHandler<StartDiningSessionCommand, Result<IssuedDiningSession>>, StartDiningSessionCommandHandler>();
        services.AddScoped<IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>>, ResolveDiningSessionQueryHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>, PlaceOrderCommandHandler>();
        services.AddScoped<IQueryHandler<GetGuestOrderQuery, Result<GuestOrderDetail>>, GetGuestOrderQueryHandler>();
        services.AddScoped<ICommandHandler<CancelGuestOrderCommand, Result<OrderTransitionResponse>>, CancelGuestOrderCommandHandler>();
        services.AddScoped<OrderTransitionService>();
        services.AddScoped<ICommandHandler<AcceptOrderCommand, Result<OrderTransitionResponse>>, AcceptOrderCommandHandler>();
        services.AddScoped<ICommandHandler<RejectOrderCommand, Result<OrderTransitionResponse>>, RejectOrderCommandHandler>();
        services.AddScoped<ICommandHandler<StartPreparingOrderCommand, Result<OrderTransitionResponse>>, StartPreparingOrderCommandHandler>();
        services.AddScoped<ICommandHandler<MarkOrderReadyCommand, Result<OrderTransitionResponse>>, MarkOrderReadyCommandHandler>();
        services.AddScoped<ICommandHandler<MarkOrderServedCommand, Result<OrderTransitionResponse>>, MarkOrderServedCommandHandler>();
        services.AddScoped<ICommandHandler<CompleteOrderCommand, Result<OrderTransitionResponse>>, CompleteOrderCommandHandler>();
        services.AddScoped<ICommandHandler<CancelOrderCommand, Result<OrderTransitionResponse>>, CancelOrderCommandHandler>();
        services.AddScoped<OrderQueueQueryService>();
        services.AddScoped<IQueryHandler<GetKitchenQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>, GetKitchenQueueQueryHandler>();
        services.AddScoped<IQueryHandler<GetCashierQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>, GetCashierQueueQueryHandler>();
        services.AddScoped<IQueryHandler<GetWaiterQueueQuery, Result<IReadOnlyList<OrderQueueItem>>>, GetWaiterQueueQueryHandler>();
        services.AddScoped<IQueryHandler<GetOrderTimelineQuery, Result<IReadOnlyList<OrderTimelineEntry>>>, GetOrderTimelineQueryHandler>();
        if (messagingOptions is not null)
        {
            if (!Uri.TryCreate(messagingOptions.ConnectionString, UriKind.Absolute, out _) ||
                messagingOptions.BatchSize <= 0 || messagingOptions.PollingInterval <= TimeSpan.Zero ||
                messagingOptions.InitialRetryDelay <= TimeSpan.Zero || messagingOptions.MaximumAttempts <= 0 ||
                messagingOptions.ClaimDuration <= TimeSpan.Zero)
                throw new ArgumentException("Messaging options are invalid.", nameof(messagingOptions));
            services.AddSingleton(messagingOptions);
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
            services.AddScoped<OutboxDispatcher>();
            services.AddScoped<InboxProcessor>();
            services.AddHostedService<OutboxPublisherWorker>();
        }
        return services;
    }
}
