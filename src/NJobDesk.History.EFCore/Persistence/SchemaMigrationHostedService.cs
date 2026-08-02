using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NJobDesk.History.EFCore.Persistence;

/// <summary>
/// Applies any pending EF Core migrations for <typeparamref name="TContext"/> at startup.
/// </summary>
internal sealed class SchemaMigrationHostedService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILogger<SchemaMigrationHostedService<TContext>> logger)
    : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Applying {Count} {Context} migration(s): {Migrations}",
            pending.Count,
            typeof(TContext).Name,
            string.Join(", ", pending));
        await context.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
