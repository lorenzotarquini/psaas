using FinHorizon.MySync.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.OwnerId).IsRequired();
        builder.Property(category => category.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(category => category.Name).HasMaxLength(Category.MaxNameLength).IsRequired();
        builder.Property(category => category.NormalizedName).HasMaxLength(Category.MaxNameLength).IsRequired();
        // UC15-R1: existing categories are variable.
        builder.Property(category => category.Fixed).HasDefaultValue(false);
        // UC6-R2, R4: the guard that makes creation idempotent under concurrency.
        builder.HasIndex(category => new { category.OwnerId, category.Kind, category.NormalizedName }).IsUnique();
    }
}
