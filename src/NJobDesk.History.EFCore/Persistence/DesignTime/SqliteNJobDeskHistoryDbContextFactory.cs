using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NJobDesk.History.EFCore.Persistence.DesignTime;

internal sealed class SqliteNJobDeskHistoryDbContextFactory : IDesignTimeDbContextFactory<SqliteNJobDeskHistoryDbContext>
{
    public SqliteNJobDeskHistoryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqliteNJobDeskHistoryDbContext>()
            .UseSqlite(
                "Data Source=design.sqlite.db",
                sql => sql.MigrationsHistoryTable(NJobDeskHistoryDbContext.MigrationsTableName))
            .Options);
}
