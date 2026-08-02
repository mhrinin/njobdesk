namespace NJobDesk.Core.Providers;

/// <summary>The scheduler providers registered with the dashboard, in registration order.</summary>
public interface ISchedulerProviderRegistry
{
    /// <summary>All registered providers.</summary>
    IReadOnlyList<ISchedulerProvider> Providers { get; }

    /// <summary>Finds a provider by its descriptor key, or <c>null</c> when unknown.</summary>
    /// <param name="key">The provider key.</param>
    ISchedulerProvider? Find(string key);
}
