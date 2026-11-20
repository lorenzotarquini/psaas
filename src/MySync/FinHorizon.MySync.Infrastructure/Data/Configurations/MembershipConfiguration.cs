using FinHorizon.MySync.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>UC17: one row per user who ever started a payment; the Stripe customer is unique too.</summary>
internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(membership => membership.OwnerId);
        builder.Property(membership => membership.CustomerId).IsRequired().HasMaxLength(255);
        builder.HasIndex(membership => membership.CustomerId).IsUnique();
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(membership => membership.SubscriptionId).HasMaxLength(255);
        builder.Property(membership => membership.PayUrl).HasMaxLength(2048);
        builder.OwnsOne(membership => membership.Card, card =>
        {
            card.Property(item => item.Brand).HasMaxLength(30);
            card.Property(item => item.Last4).HasMaxLength(4);
        });
        builder.OwnsOne(membership => membership.Plan, plan =>
        {
            plan.Property(item => item.Name).HasMaxLength(250);
            plan.Property(item => item.Amount).HasPrecision(12, 2);
            plan.Property(item => item.Currency).HasMaxLength(3);
            plan.Property(item => item.Interval).HasMaxLength(5);
        });
        builder.OwnsOne(membership => membership.LastFailure, failure =>
        {
            failure.Property(item => item.Code).HasMaxLength(100);
            failure.Property(item => item.Message).HasMaxLength(500);
        });
    }
}
