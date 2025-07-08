using Discounts.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Discounts.Server.Services;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscountCode>(t =>
        {
            t.HasKey(x => x.Code);

            t.Property(x => x.Code)
                .HasMaxLength(8)
                .HasColumnName("code");

            t.Property(x => x.Redeemed)
                .HasColumnName("redeemed")
                .HasDefaultValue(false);

            t.Property(u => u.Version)
                .HasColumnName("version")
                .IsConcurrencyToken();

            t.ToTable("discount_codes");
        });
    }
}
