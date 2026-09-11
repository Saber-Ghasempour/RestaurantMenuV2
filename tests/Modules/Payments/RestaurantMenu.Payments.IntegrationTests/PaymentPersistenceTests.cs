using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Payments.IntegrationTests;

public sealed class PaymentPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _database.StartAsync();
    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrationPersistsLedgerAndEnforcesOnePaymentPerOrder()
    {
        var options = Options();
        var orderId = Guid.NewGuid();
        await using (var setup = new PaymentsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Payments.Add(CreatePayment(orderId));
            await setup.SaveChangesAsync();
        }

        await using (var duplicate = new PaymentsDbContext(options))
        {
            duplicate.Payments.Add(CreatePayment(orderId));
            await Assert.ThrowsAsync<DuplicatePaymentException>(() => duplicate.SaveChangesAsync());
        }

        await using var verify = new PaymentsDbContext(options);
        var payment = await verify.Payments.Include(value => value.Events).SingleAsync();
        Assert.Equal(13_700, payment.GrossAmountMinor);
        Assert.Equal(411, payment.PlatformFeeAmountMinor);
        Assert.Equal(3, payment.Events.Count);
    }

    private DbContextOptions<PaymentsDbContext> Options() =>
        new DbContextOptionsBuilder<PaymentsDbContext>().UseNpgsql(_database.GetConnectionString()).Options;

    private static Payment CreatePayment(Guid orderId)
    {
        var now = DateTimeOffset.UtcNow;
        var payment = Payment.Create(PaymentId.New(), Guid.NewGuid(), Guid.NewGuid(), orderId,
            Guid.NewGuid(), "acct_restaurant", new BillAmount(10_000, 500, 2_300, 1_200, 700, "EUR"),
            300, now).Value;
        payment.AttachProviderIntent("pi_" + orderId.ToString("N"), "created:" + orderId.ToString("N"), now);
        payment.MarkSucceeded("succeeded:" + orderId.ToString("N"), now);
        return payment;
    }
}
