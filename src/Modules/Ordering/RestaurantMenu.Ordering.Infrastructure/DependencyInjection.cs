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
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services,
        string connectionString, TimeSpan sessionLifetime)
    {
        if (sessionLifetime <= TimeSpan.Zero || sessionLifetime > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(sessionLifetime), "Dining-session lifetime must be between zero and 24 hours.");
        services.AddDbContext<OrderingDbContext>(options => options.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "ordering")));
        services.AddScoped<IDiningSessionRepository, DiningSessionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IOrderingUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());
        services.AddSingleton<IDiningSessionTokenGenerator, CryptographicDiningSessionTokenGenerator>();
        services.AddSingleton(new DiningSessionOptions(sessionLifetime));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICommandHandler<StartDiningSessionCommand, Result<IssuedDiningSession>>, StartDiningSessionCommandHandler>();
        services.AddScoped<IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>>, ResolveDiningSessionQueryHandler>();
        services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>, PlaceOrderCommandHandler>();
        return services;
    }
}
