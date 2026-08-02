#if NET10_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using NJobDesk.History.EFCore.Persistence;

namespace NJobDesk.Tests.History;

public class MigrationParityTests
{
    [Fact]
    public void Sqlite_migrations_match_the_model() =>
        Assert.False(new SqliteNJobDeskHistoryDbContext(
                new DbContextOptionsBuilder<SqliteNJobDeskHistoryDbContext>()
                    .UseSqlite("Data Source=parity.sqlite.db")
                    .Options)
            .Database.HasPendingModelChanges());

    [Fact]
    public void SqlServer_migrations_match_the_model() =>
        Assert.False(new SqlServerNJobDeskHistoryDbContext(
                new DbContextOptionsBuilder<SqlServerNJobDeskHistoryDbContext>()
                    .UseSqlServer("Server=(local);Database=parity;Trusted_Connection=True;")
                    .Options)
            .Database.HasPendingModelChanges());
}
#endif
