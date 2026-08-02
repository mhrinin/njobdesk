namespace NJobDesk.History.EFCore;

/// <summary>The database the execution history is stored in.</summary>
public sealed record HistoryDatabase(HistoryDatabaseProvider Provider, string? ConnectionString)
{
    private const string SqlServerProviderName = "Microsoft.Data.SqlClient";
    private const string LegacySqlServerProviderName = "System.Data.SqlClient";
    private const string SqliteProviderName = "Microsoft.Data.Sqlite";

    /// <summary>Creates a SQL Server history database.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public static HistoryDatabase SqlServer(string connectionString) =>
        new(HistoryDatabaseProvider.SqlServer, connectionString);

    /// <summary>Creates a SQLite history database.</summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public static HistoryDatabase Sqlite(string connectionString) =>
        new(HistoryDatabaseProvider.Sqlite, connectionString);

    /// <summary>
    /// Infers the provider from an ADO.NET provider name (e.g. from a host's connection-string
    /// configuration). Unknown providers map to <see cref="HistoryDatabaseProvider.None"/>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="providerName">The ADO.NET provider invariant name.</param>
    public static HistoryDatabase FromConnectionString(string? connectionString, string? providerName)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new HistoryDatabase(HistoryDatabaseProvider.None, null);
        }

        var provider = providerName switch
        {
            SqlServerProviderName or LegacySqlServerProviderName => HistoryDatabaseProvider.SqlServer,
            SqliteProviderName => HistoryDatabaseProvider.Sqlite,
            _ => HistoryDatabaseProvider.None,
        };

        return new HistoryDatabase(provider, provider is HistoryDatabaseProvider.None ? null : connectionString);
    }
}
