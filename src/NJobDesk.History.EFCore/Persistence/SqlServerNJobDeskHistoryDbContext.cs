using Microsoft.EntityFrameworkCore;
using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Persistence;

public sealed class SqlServerNJobDeskHistoryDbContext(DbContextOptions<SqlServerNJobDeskHistoryDbContext> options)
    : NJobDeskHistoryDbContext(options)
{
    public const string Schema = "njobdesk";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JobExecutionHistory>().ToTable(HistoryTableName, Schema);
        modelBuilder.Entity<JobExecutionLog>().ToTable(ExecutionLogTableName, Schema);
    }
}
