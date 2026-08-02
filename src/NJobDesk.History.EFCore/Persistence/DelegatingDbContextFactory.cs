using Microsoft.EntityFrameworkCore;

namespace NJobDesk.History.EFCore.Persistence;

/// <summary>
/// Adapts a provider-specific <see cref="IDbContextFactory{TConcrete}"/> to the abstract context
/// type its consumers depend on, so services never see the concrete provider subclass.
/// </summary>
internal sealed class DelegatingDbContextFactory<TConcrete, TAbstract>(IDbContextFactory<TConcrete> inner)
    : IDbContextFactory<TAbstract>
    where TConcrete : TAbstract
    where TAbstract : DbContext
{
    public TAbstract CreateDbContext() => inner.CreateDbContext();

    public async Task<TAbstract> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        await inner.CreateDbContextAsync(cancellationToken);
}
