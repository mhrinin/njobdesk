using Microsoft.EntityFrameworkCore;
using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Persistence;

public abstract class NJobDeskHistoryDbContext(DbContextOptions options) : DbContext(options)
{
    public const string HistoryTableName = "NJobDeskExecutionHistory";
    public const string ExecutionLogTableName = "NJobDeskExecutionLog";
    public const string MigrationsTableName = "__NJobDeskHistoryMigrations";

    public DbSet<JobExecutionHistory> ExecutionHistory => Set<JobExecutionHistory>();

    public DbSet<JobExecutionLog> ExecutionLogs => Set<JobExecutionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var history = modelBuilder.Entity<JobExecutionHistory>();
        history.ToTable(HistoryTableName);
        history.HasKey(x => x.Id);
        history.Property(x => x.FireInstanceId).HasMaxLength(140);
        history.Property(x => x.SchedulerInstanceId).HasMaxLength(200);
        history.Property(x => x.ProviderKey).HasMaxLength(64);
        history.Property(x => x.JobId).HasMaxLength(400);
        history.Property(x => x.JobName).HasMaxLength(150);
        history.Property(x => x.JobGroup).HasMaxLength(150);
        history.Property(x => x.TriggerId).HasMaxLength(400);
        history.Property(x => x.TriggerName).HasMaxLength(150);
        history.Property(x => x.ErrorMessage).HasMaxLength(4000);
        history.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        history.HasIndex(x => new { x.ProviderKey, x.FireInstanceId }).IsUnique();
        history.HasIndex(x => new { x.ProviderKey, x.JobId, x.StartedUtc });
        history.HasIndex(x => x.StartedUtc);
        history.HasIndex(x => x.Status);

        var log = modelBuilder.Entity<JobExecutionLog>();
        log.ToTable(ExecutionLogTableName);
        log.HasKey(x => x.Id);
        log.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
        log.Property(x => x.Category).HasMaxLength(512);
        log.HasIndex(x => x.ExecutionId);
        log.HasOne<JobExecutionHistory>()
            .WithMany()
            .HasForeignKey(x => x.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
