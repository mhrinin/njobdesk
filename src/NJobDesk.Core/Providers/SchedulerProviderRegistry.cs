using System.Text.RegularExpressions;

namespace NJobDesk.Core.Providers;

internal sealed partial class SchedulerProviderRegistry : ISchedulerProviderRegistry
{
    private readonly Dictionary<string, ISchedulerProvider> _byKey;

    public SchedulerProviderRegistry(IEnumerable<ISchedulerProvider> providers)
    {
        Providers = [.. providers];
        _byKey = new Dictionary<string, ISchedulerProvider>(Providers.Count, StringComparer.Ordinal);
        foreach (var provider in Providers)
        {
            var key = provider.Descriptor.Key;
            if (!ProviderKeyPattern().IsMatch(key))
            {
                throw new InvalidOperationException(
                    $"Scheduler provider key '{key}' is invalid. Keys must be lowercase letters, digits, and dashes.");
            }

            if (!_byKey.TryAdd(key, provider))
            {
                throw new InvalidOperationException($"Scheduler provider key '{key}' is registered more than once.");
            }
        }
    }

    public IReadOnlyList<ISchedulerProvider> Providers { get; }

    public ISchedulerProvider? Find(string key) => _byKey.GetValueOrDefault(key);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex ProviderKeyPattern();
}
