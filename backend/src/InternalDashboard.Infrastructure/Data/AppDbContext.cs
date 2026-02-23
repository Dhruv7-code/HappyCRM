using InternalDashboard.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace InternalDashboard.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer>       Customers       => Set<Customer>();
    public DbSet<IntegrationJob> IntegrationJobs => Set<IntegrationJob>();
    public DbSet<JobExecution>   JobExecutions   => Set<JobExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Customer ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(256);
            e.Property(c => c.Email).IsRequired().HasMaxLength(256);
            e.HasIndex(c => c.Email).IsUnique();
        });

        // ── IntegrationJob ────────────────────────────────────────────────────
        modelBuilder.Entity<IntegrationJob>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.JobName).IsRequired().HasMaxLength(256);
            e.Property(j => j.Status).HasConversion<string>(); // stored as string in DB
            e.Property(j => j.RetryCount).HasDefaultValue(0);
            e.HasOne(j => j.Customer)
             .WithMany(c => c.IntegrationJobs)
             .HasForeignKey(j => j.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── JobExecution ──────────────────────────────────────────────────────
        modelBuilder.Entity<JobExecution>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.FailureType).HasConversion<string>();
            e.HasOne(x => x.IntegrationJob)
             .WithMany(j => j.JobExecutions)
             .HasForeignKey(x => x.IntegrationJobId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
