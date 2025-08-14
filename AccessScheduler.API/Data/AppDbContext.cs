using Microsoft.EntityFrameworkCore;
using AccessScheduler.Shared.Models;

namespace AccessScheduler.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");

            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Document)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Resource)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.StartUtc)
                .IsRequired();

            entity.Property(e => e.EndUtc)
                .IsRequired();

            entity.Property(e => e.Latitude)
                .IsRequired()
                .HasPrecision(10, 8);

            entity.Property(e => e.Longitude)
                .IsRequired()
                .HasPrecision(11, 8);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.RowVersion)
                .IsRowVersion();

            entity.HasIndex(e => new { e.Resource, e.StartUtc, e.EndUtc })
                .HasDatabaseName("IX_Bookings_Resource_TimeRange");
        });

        base.OnModelCreating(modelBuilder);
    }
}