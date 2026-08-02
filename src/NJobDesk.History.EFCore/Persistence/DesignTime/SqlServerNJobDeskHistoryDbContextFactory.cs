using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NJobDesk.History.EFCore.Persistence.DesignTime;

internal sealed class SqlServerNJobDeskHistoryDbContextFactory : IDesignTimeDbContextFactory<SqlServerNJobDeskHistoryDbContext>
{
    public SqlServerNJobDeskHistoryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqlServerNJobDeskHistoryDbContext>()
            .UseSqlServer(
                "Server=(local);Database=design;Trusted_Connection=True;",
                sql => sql.MigrationsHistoryTable(
                    NJobDeskHistoryDbContext.MigrationsTableName,
                    SqlServerNJobDeskHistoryDbContext.Schema))
            .Options);
}
