using System.Linq.Expressions;
using FinHorizon.MySync.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinHorizon.MySync.Infrastructure.Data.Configurations;

/// <summary>Name, description and tag of accounts and debts, as plain columns of their table.</summary>
internal static class DescriptorMapping
{
    public static void MapDescriptor<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, Descriptor?>> descriptor)
        where TEntity : class =>
        builder.ComplexProperty<Descriptor>(descriptor, complex =>
        {
            complex.Property(d => d.Name).HasColumnName("Name").HasMaxLength(Descriptor.MaxNameLength).IsRequired();
            complex.Property(d => d.Description).HasColumnName("Description").HasMaxLength(Descriptor.MaxDescriptionLength);
            complex.Property(d => d.Tag).HasColumnName("Tag").HasMaxLength(Descriptor.MaxTagLength);
            complex.Ignore(d => d.NormalizedName);
        });
}
