using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantMenu.Payments.Domain.Payments;

namespace RestaurantMenu.Payments.Infrastructure.Database;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table =>
        {
            table.HasCheckConstraint("ck_payments_components", "subtotal_amount_minor >= 0 AND discount_amount_minor >= 0 AND discount_amount_minor <= subtotal_amount_minor AND tax_amount_minor >= 0 AND tip_amount_minor >= 0 AND other_fee_amount_minor >= 0");
            table.HasCheckConstraint("ck_payments_gross", "gross_amount_minor = subtotal_amount_minor - discount_amount_minor + tax_amount_minor + tip_amount_minor + other_fee_amount_minor AND gross_amount_minor > 0");
            table.HasCheckConstraint("ck_payments_commission", "commission_rate_basis_points BETWEEN 1 AND 10000 AND platform_fee_amount_minor >= 0 AND platform_fee_amount_minor <= gross_amount_minor AND restaurant_proceeds_amount_minor = gross_amount_minor - platform_fee_amount_minor");
            table.HasCheckConstraint("ck_payments_refunds", "refunded_amount_minor BETWEEN 0 AND gross_amount_minor AND refunded_platform_fee_amount_minor BETWEEN 0 AND platform_fee_amount_minor");
            table.HasCheckConstraint("ck_payments_currency", "currency ~ '^[A-Z]{3}$'");
            table.HasCheckConstraint("ck_payments_status", "status IN ('Pending', 'Processing', 'Succeeded', 'PartiallyRefunded', 'Refunded', 'Failed', 'Cancelled')");
        });
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).HasConversion(id => id.Value, value => new PaymentId(value)).HasColumnName("id").ValueGeneratedNever();
        builder.Property(payment => payment.RestaurantId).HasColumnName("restaurant_id").IsRequired();
        builder.Property(payment => payment.BranchId).HasColumnName("branch_id").IsRequired();
        builder.Property(payment => payment.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(payment => payment.DiningSessionId).HasColumnName("dining_session_id").IsRequired();
        builder.Property(payment => payment.ConnectedAccountId).HasColumnName("connected_account_id").HasMaxLength(Payment.MaxProviderReferenceLength).IsRequired();
        builder.Property(payment => payment.SubtotalAmountMinor).HasColumnName("subtotal_amount_minor").IsRequired();
        builder.Property(payment => payment.DiscountAmountMinor).HasColumnName("discount_amount_minor").IsRequired();
        builder.Property(payment => payment.TaxAmountMinor).HasColumnName("tax_amount_minor").IsRequired();
        builder.Property(payment => payment.TipAmountMinor).HasColumnName("tip_amount_minor").IsRequired();
        builder.Property(payment => payment.OtherFeeAmountMinor).HasColumnName("other_fee_amount_minor").IsRequired();
        builder.Property(payment => payment.GrossAmountMinor).HasColumnName("gross_amount_minor").IsRequired();
        builder.Property(payment => payment.Currency).HasColumnName("currency").HasColumnType("character(3)").IsRequired();
        builder.Property(payment => payment.CommissionRateBasisPoints).HasColumnName("commission_rate_basis_points").IsRequired();
        builder.Property(payment => payment.PlatformFeeAmountMinor).HasColumnName("platform_fee_amount_minor").IsRequired();
        builder.Property(payment => payment.RestaurantProceedsAmountMinor).HasColumnName("restaurant_proceeds_amount_minor").IsRequired();
        builder.Property(payment => payment.ProviderPaymentIntentId).HasColumnName("provider_payment_intent_id").HasMaxLength(Payment.MaxProviderReferenceLength);
        builder.Property(payment => payment.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(payment => payment.RefundedAmountMinor).HasColumnName("refunded_amount_minor").IsRequired();
        builder.Property(payment => payment.RefundedPlatformFeeAmountMinor).HasColumnName("refunded_platform_fee_amount_minor").IsRequired();
        builder.Property(payment => payment.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(payment => payment.SucceededAtUtc).HasColumnName("succeeded_at_utc").HasColumnType("timestamp with time zone");
        builder.Property(payment => payment.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken().IsRequired();
        builder.HasIndex(payment => payment.OrderId).IsUnique().HasDatabaseName("ux_payments_order");
        builder.HasIndex(payment => payment.ProviderPaymentIntentId).IsUnique().HasFilter("provider_payment_intent_id IS NOT NULL").HasDatabaseName("ux_payments_provider_intent");
        builder.HasIndex(payment => new { payment.RestaurantId, payment.CreatedAtUtc }).HasDatabaseName("ix_payments_restaurant_created");
        builder.HasMany(payment => payment.Events).WithOne().HasForeignKey(paymentEvent => paymentEvent.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(payment => payment.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(payment => payment.DomainEvents);
    }
}

public sealed class PaymentEventConfiguration : IEntityTypeConfiguration<PaymentEvent>
{
    public void Configure(EntityTypeBuilder<PaymentEvent> builder)
    {
        builder.ToTable("payment_events", table =>
        {
            table.HasCheckConstraint("ck_payment_events_amount", "amount_minor IS NULL OR amount_minor >= 0");
            table.HasCheckConstraint("ck_payment_events_fee", "platform_fee_amount_minor IS NULL OR platform_fee_amount_minor >= 0");
        });
        builder.HasKey(paymentEvent => paymentEvent.Id);
        builder.Property(paymentEvent => paymentEvent.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(paymentEvent => paymentEvent.PaymentId).HasConversion(id => id.Value, value => new PaymentId(value)).HasColumnName("payment_id").IsRequired();
        builder.Property(paymentEvent => paymentEvent.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
        builder.Property(paymentEvent => paymentEvent.ProviderEventId).HasColumnName("provider_event_id").HasMaxLength(Payment.MaxProviderReferenceLength);
        builder.Property(paymentEvent => paymentEvent.AmountMinor).HasColumnName("amount_minor");
        builder.Property(paymentEvent => paymentEvent.PlatformFeeAmountMinor).HasColumnName("platform_fee_amount_minor");
        builder.Property(paymentEvent => paymentEvent.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(paymentEvent => paymentEvent.ProviderEventId).IsUnique().HasFilter("provider_event_id IS NOT NULL").HasDatabaseName("ux_payment_events_provider_event");
        builder.HasIndex(paymentEvent => new { paymentEvent.PaymentId, paymentEvent.OccurredAtUtc }).HasDatabaseName("ix_payment_events_payment_occurred");
    }
}
