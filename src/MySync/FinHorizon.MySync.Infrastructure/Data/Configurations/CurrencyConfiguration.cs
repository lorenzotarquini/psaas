using FinHorizon.MySync.Domain.Preferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>One row per user who chose a display currency (UC13-R4).</summary>
internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    /// <summary>ISO codes have 3 letters, crypto tickers up to 10 (CurrencyRules.IsWellFormed).</summary>
    public const int CodeLength = 10;

    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("Preferences");
        builder.HasKey(preferences => preferences.Id);
        builder.Property(preferences => preferences.Id).ValueGeneratedNever();
        builder.Property(preferences => preferences.OwnerId).IsRequired();
        builder.Property(preferences => preferences.DisplayCurrency).HasMaxLength(CodeLength).IsRequired();
        builder.HasIndex(preferences => preferences.OwnerId).IsUnique();
    }
}