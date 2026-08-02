using Microsoft.Extensions.DependencyInjection;
using NJobDesk.Core.Providers;

namespace NJobDesk.Core.DependencyInjection;

/// <summary>
/// Composition surface returned by <c>AddNJobDesk</c>. Feature packages (history, persistence, web)
/// extend NJobDesk through extension methods on this type; <see cref="SectionName"/> carries the
/// configuration section all NJobDesk options bind from.
/// </summary>
public sealed class NJobDeskBuilder
{
    internal NJobDeskBuilder(IServiceCollection services, string sectionName)
    {
        Services = services;
        SectionName = sectionName;
    }

    public IServiceCollection Services { get; }

    public string SectionName { get; }

    /// <summary>
    /// Plugs a scheduler provider into the dashboard. Multiple providers can be registered; their
    /// jobs are aggregated and tagged with the provider key.
    /// </summary>
    /// <typeparam name="TProvider">The provider implementation.</typeparam>
    public NJobDeskBuilder AddProvider<TProvider>()
        where TProvider : class, ISchedulerProvider
    {
        Services.AddSingleton<ISchedulerProvider, TProvider>();
        return this;
    }
}
