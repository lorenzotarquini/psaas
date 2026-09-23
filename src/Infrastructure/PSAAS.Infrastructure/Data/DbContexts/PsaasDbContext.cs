using Microsoft.EntityFrameworkCore;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Data.DbContexts;

public sealed class PsaasDbContext(DbContextOptions<PsaasDbContext> options) : DbContext(options)
{
    public DbSet<TenantDbModel> Tenants => Set<TenantDbModel>();

    public DbSet<ProductiveUnitDbModel> ProductiveUnits => Set<ProductiveUnitDbModel>();

    public DbSet<ProfileDbModel> Profiles => Set<ProfileDbModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Tenants");

            entityBuilder.HasKey(tenant => tenant.Id);

            entityBuilder.HasIndex(tenant => tenant.Id).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.VatNumber).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.CompleteVatNumber).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.CertifiedEmail).IsUnique();

            entityBuilder.Property(tenant => tenant.Id).IsRequired();
            entityBuilder.Property(tenant => tenant.CompanyName).IsRequired();
            entityBuilder.Property(tenant => tenant.VatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CompleteVatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CertifiedEmail).IsRequired();

            entityBuilder.OwnsOne(tenant => tenant.Address, addressBuilder =>
            {
                addressBuilder.ToJson(nameof(TenantDbModel.Address));
            });
        });

        modelBuilder.Entity<ProductiveUnitDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("ProductiveUnits");

            entityBuilder.HasKey(productiveUnit => productiveUnit.Id);

            entityBuilder.HasIndex(productiveUnit => productiveUnit.Name).IsUnique();

            entityBuilder.Property(productiveUnit => productiveUnit.Id).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Name).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Description).IsRequired(false);
            entityBuilder.Property(productiveUnit => productiveUnit.TenantId).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.ProfileId).IsRequired();

            entityBuilder
                .HasOne(productiveUnit => productiveUnit.Tenant)
                .WithMany()
                .HasForeignKey(productiveUnit => productiveUnit.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder
                .HasOne(productiveUnit => productiveUnit.Profile)
                .WithMany(profile => profile.ProductiveUnits)
                .HasForeignKey(productiveUnit => productiveUnit.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Profiles");

            entityBuilder.HasKey(profile => profile.Id);

            entityBuilder.HasIndex(profile => profile.Id).IsUnique();
            entityBuilder.HasIndex(profile => profile.Name).IsUnique();

            entityBuilder.Property(profile => profile.Id).IsRequired();
            entityBuilder.Property(profile => profile.Name).IsRequired();
            entityBuilder.Property(profile => profile.Description).IsRequired(false);
        });

        base.OnModelCreating(modelBuilder);
    }
}
