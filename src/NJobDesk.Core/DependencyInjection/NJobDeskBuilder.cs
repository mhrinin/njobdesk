using Microsoft.Extensions.DependencyInjection;

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
}
