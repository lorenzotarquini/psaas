using FinHorizon.MySync.Domain.Projection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>UC14-R11: one row per user; payments and events are small lists kept as JSON next to it.</summary>
internal sealed class ProjectionSettingsConfiguration : IEntityTypeConfiguration<ProjectionSettings>
{
    public void Configure(EntityTypeBuilder<ProjectionSettings> builder)
    {
        builder.ToTable("ProjectionSettings");
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.OwnerId).IsRequired();
        builder.HasIndex(settings => settings.OwnerId).IsUnique();
        foreach (var amount in new[] { nameof(ProjectionSettings.Income), nameof(ProjectionSettings.FixedExpenses), nameof(ProjectionSettings.VariableExpenses), nameof(ProjectionSettings.Saving), nameof(ProjectionSettings.Goal) })
        {
            builder.Property(amount).HasPrecision(18, 2);
        }

        builder.Property(settings => settings.AnnualReturn).HasPrecision(6, 3);
        builder.Property(settings => settings.Inflation).HasPrecision(6, 3);
        builder.OwnsMany(settings => settings.Payments, payments => payments.ToJson());
        builder.OwnsMany(settings => settings.Events, events =>
        {
            events.ToJson();
            events.Property(item => item.Note).HasMaxLength(ProjectionSettings.MaxNoteLength);
        });
        builder.Navigation(settings => settings.Payments).HasField("_payments").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(settings => settings.Events).HasField("_events").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
