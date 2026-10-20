using Microsoft.EntityFrameworkCore;
using PSAAS.Infrastructure.Data.DbModels;

namespace PSAAS.Infrastructure.Data.DbContexts;

public sealed class PsaasDbContext(DbContextOptions<PsaasDbContext> options) : DbContext(options)
{
    public DbSet<TenantDbModel> Tenants => Set<TenantDbModel>();

    public DbSet<ProductiveUnitDbModel> ProductiveUnits => Set<ProductiveUnitDbModel>();

    public DbSet<ProfileDbModel> Profiles => Set<ProfileDbModel>();

    public DbSet<ProductiveUnitProfileDbModel> ProductiveUnitProfiles => Set<ProductiveUnitProfileDbModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Tenants");

            entityBuilder.HasKey(tenant => tenant.Id);

            entityBuilder.HasIndex(tenant => tenant.Id).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.NormalizedCompanyName).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.CompleteVatNumber).IsUnique();
            entityBuilder.HasIndex(tenant => tenant.NormalizedCertifiedEmail).IsUnique();

            entityBuilder.Property(tenant => tenant.Id).IsRequired();
            entityBuilder.Property(tenant => tenant.CompanyName).IsRequired();
            entityBuilder.Property(tenant => tenant.NormalizedCompanyName).IsRequired();
            entityBuilder.Property(tenant => tenant.VatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CompleteVatNumber).IsRequired();
            entityBuilder.Property(tenant => tenant.CertifiedEmail).IsRequired();
            entityBuilder.Property(tenant => tenant.NormalizedCertifiedEmail).IsRequired();

            entityBuilder.OwnsOne(tenant => tenant.Address, addressBuilder =>
            {
                addressBuilder.ToJson(nameof(TenantDbModel.Address));
            });
        });

        modelBuilder.Entity<ProductiveUnitDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("ProductiveUnits");

            entityBuilder.HasKey(productiveUnit => productiveUnit.Id);

            entityBuilder.HasIndex(productiveUnit => new { productiveUnit.TenantId, productiveUnit.Name }).IsUnique();

            entityBuilder.Property(productiveUnit => productiveUnit.Id).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Name).IsRequired();
            entityBuilder.Property(productiveUnit => productiveUnit.Description).IsRequired(false);
            entityBuilder.Property(productiveUnit => productiveUnit.TenantId).IsRequired();
            entityBuilder
                .HasOne(productiveUnit => productiveUnit.Tenant)
                .WithMany()
                .HasForeignKey(productiveUnit => productiveUnit.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductiveUnitProfileDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("ProductiveUnitProfiles");

            entityBuilder.HasKey(link => new { link.ProductiveUnitId, link.ProfileId });

            entityBuilder.Property(link => link.ProductiveUnitId).IsRequired();
            entityBuilder.Property(link => link.ProfileId).IsRequired();

            entityBuilder
                .HasOne(link => link.ProductiveUnit)
                .WithMany(productiveUnit => productiveUnit.ProductiveUnitProfiles)
                .HasForeignKey(link => link.ProductiveUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            entityBuilder
                .HasOne(link => link.Profile)
                .WithMany(profile => profile.ProductiveUnitProfiles)
                .HasForeignKey(link => link.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProfileDbModel>(entityBuilder =>
        {
            entityBuilder.ToTable("Profiles");

            entityBuilder.HasKey(profile => profile.Id);

            entityBuilder.HasIndex(profile => profile.Id).IsUnique();
            entityBuilder.HasIndex(profile => new { profile.TenantId, profile.Name }).IsUnique();

            entityBuilder.Property(profile => profile.Id).IsRequired();
            entityBuilder.Property(profile => profile.Name).IsRequired();
            entityBuilder.Property(profile => profile.Description).IsRequired(false);
            entityBuilder.Property(profile => profile.TenantId).IsRequired();
            entityBuilder
                .HasOne<TenantDbModel>()
                .WithMany()
                .HasForeignKey(profile => profile.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
