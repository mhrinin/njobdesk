namespace NJobDesk.Core.Providers;

/// <summary>
/// Composes and splits the dashboard-level ids the API exposes: <c>{providerKey}:{localId}</c>.
/// Provider keys never contain the separator, so splitting on the first occurrence is unambiguous
/// even when the local id itself contains one.
/// </summary>
public static class CompositeId
{
    /// <summary>Separator between the provider key and the provider-local id.</summary>
    public const char Separator = ':';

    /// <summary>Builds the dashboard-level id for a provider-local id.</summary>
    /// <param name="providerKey">The provider key.</param>
    /// <param name="localId">The provider-local id.</param>
    public static string Compose(string providerKey, string localId) => $"{providerKey}{Separator}{localId}";

    /// <summary>Splits a dashboard-level id back into its provider key and provider-local id.</summary>
    /// <param name="id">The dashboard-level id.</param>
    /// <param name="providerKey">The provider key, when the id is well-formed.</param>
    /// <param name="localId">The provider-local id, when the id is well-formed.</param>
    public static bool TrySplit(string? id, out string providerKey, out string localId)
    {
        providerKey = string.Empty;
        localId = string.Empty;
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        var separatorIndex = id.IndexOf(Separator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == id.Length - 1)
        {
            return false;
        }

        providerKey = id[..separatorIndex];
        localId = id[(separatorIndex + 1)..];
        return true;
    }
}
