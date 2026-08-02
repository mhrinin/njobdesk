using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NJobDesk.History.EFCore.Persistence;

namespace NJobDesk.Tests.History;

/// <summary>
/// An in-memory SQLite history database (kept alive by an open connection) with the real schema
/// migrations applied, exposed through the same factory abstraction the stores use.
/// </summary>
internal sealed class SqliteHistoryDatabaseFixture : IDbContextFactory<NJobDeskHistoryDbContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<SqliteNJobDeskHistoryDbContext> _options;

    public SqliteHistoryDatabaseFixture()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<SqliteNJobDeskHistoryDbContext>()
            .UseSqlite(
                _connection,
                sql => sql.MigrationsHistoryTable(NJobDeskHistoryDbContext.MigrationsTableName))
            .Options;
        using var context = new SqliteNJobDeskHistoryDbContext(_options);
        context.Database.Migrate();
    }

    public NJobDeskHistoryDbContext CreateDbContext() => new SqliteNJobDeskHistoryDbContext(_options);

    public Task<NJobDeskHistoryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
