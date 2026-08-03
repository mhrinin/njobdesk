using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJobDesk.Core.DependencyInjection;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Capture;
using NJobDesk.History.EFCore.Configuration;
using NJobDesk.History.EFCore.Maintenance;
using NJobDesk.History.EFCore.Persistence;
using NJobDesk.History.EFCore.Store;

namespace NJobDesk.History.EFCore.DependencyInjection;

/// <summary>Registers EF Core execution-history storage for the NJobDesk dashboard.</summary>
public static class NJobDeskEfHistoryExtensions
{
    /// <summary>
    /// Stores execution history (and per-run <c>ILogger</c> capture) in the given SQL Server or
    /// SQLite database. Applies the schema migrations at startup, reconciles runs a previous process
    /// left in Running, and prunes finished runs past the retention window on a timer. Options bind
    /// from the <c>History</c> section under the NJobDesk configuration section and can be adjusted
    /// with <paramref name="configure"/>. Safe to call multiple times; only the first registration
    /// wins.
    /// </summary>
    /// <param name="builder">The NJobDesk builder.</param>
    /// <param name="database">The database to store history in.</param>
    /// <param name="configure">Optional history option overrides applied after configuration binding.</param>
    public static NJobDeskBuilder AddEfHistory(
        this NJobDeskBuilder builder,
        HistoryDatabase database,
        Action<NJobDeskHistoryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (database.Provider is HistoryDatabaseProvider.None)
        {
            throw new ArgumentException("Execution history requires a SQL Server or SQLite connection.", nameof(database));
        }

        var services = builder.Services;
        services.AddOptions<NJobDeskHistoryOptions>();
        // Bind tolerantly: hosts without IConfiguration (bare service collections) keep the defaults.
        services.AddSingleton<IConfigureOptions<NJobDeskHistoryOptions>>(provider =>
            new ConfigureNamedOptions<NJobDeskHistoryOptions>(Options.DefaultName, options =>
                provider.GetService<IConfiguration>()?.GetSection($"{builder.SectionName}:History").Bind(options)));
        if (configure is not null)
        {
            services.Configure(configure);
        }

        if (!TryMark<HistoryMarker>(services))
        {
            return builder;
        }

        RegisterDatabase(services, database);
        services.Replace(ServiceDescriptor.Singleton<IExecutionHistoryStore, EfExecutionHistoryStore>());
        services.Replace(ServiceDescriptor.Singleton<IExecutionHistoryWriter, EfExecutionHistoryWriter>());
        services.AddHostedService<ExecutionHistoryReconciliationService>();
        services.AddHostedService<ExecutionHistoryCleanupService>();

        services.TryAddSingleton<IExecutionLogCapture, ExecutionLogCapture>();
        services.TryAddSingleton<IExecutionLogStore, ExecutionLogStore>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ExecutionLogCaptureLoggerProvider>());

        return builder;
    }

    private static void RegisterDatabase(IServiceCollection services, HistoryDatabase database)
    {
        if (database.Provider is HistoryDatabaseProvider.SqlServer)
        {
            Register<SqlServerNJobDeskHistoryDbContext>(services, options =>
                options.UseSqlServer(
                    database.ConnectionString,
                    sql => sql.MigrationsHistoryTable(
                        NJobDeskHistoryDbContext.MigrationsTableName,
                        SqlServerNJobDeskHistoryDbContext.Schema)));
        }
        else
        {
            Register<SqliteNJobDeskHistoryDbContext>(services, options =>
                options.UseSqlite(
                    database.ConnectionString,
                    sql => sql.MigrationsHistoryTable(NJobDeskHistoryDbContext.MigrationsTableName)));
        }
    }

    private static void Register<TConcrete>(IServiceCollection services, Action<DbContextOptionsBuilder> configure)
        where TConcrete : NJobDeskHistoryDbContext
    {
        services.AddDbContextFactory<TConcrete>(configure);
        services.AddSingleton<IDbContextFactory<NJobDeskHistoryDbContext>>(provider =>
            new DelegatingDbContextFactory<TConcrete, NJobDeskHistoryDbContext>(
                provider.GetRequiredService<IDbContextFactory<TConcrete>>()));
        services.AddHostedService<SchemaMigrationHostedService<TConcrete>>();
    }

    private static bool TryMark<TMarker>(IServiceCollection services)
        where TMarker : class
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TMarker)))
        {
            return false;
        }

        services.AddSingleton<TMarker>();
        return true;
    }

    private sealed class HistoryMarker;
}
