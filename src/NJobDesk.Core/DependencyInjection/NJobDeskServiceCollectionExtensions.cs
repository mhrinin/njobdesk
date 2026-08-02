using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NJobDesk.Core.Configuration;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;

namespace NJobDesk.Core.DependencyInjection;

/// <summary>Registers the NJobDesk core services.</summary>
public static class NJobDeskServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dashboard core services: the provider registry, the aggregating dashboard
    /// services the API consumes, and scheduler-agnostic defaults (Cronos cron validation, empty
    /// history). Scheduler providers plug in through <see cref="NJobDeskBuilder.AddProvider{TProvider}"/>;
    /// without one the dashboard shows an unconfigured state. Safe to call multiple times; the first
    /// call decides the configuration section every NJobDesk option binds from. Returns a
    /// <see cref="NJobDeskBuilder"/> that feature packages extend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionName">Configuration section NJobDesk feature options bind from.</param>
    public static NJobDeskBuilder AddNJobDesk(
        this IServiceCollection services,
        string sectionName = NJobDeskSectionName.Default)
    {
        if (!TryMark<CoreMarker>(services))
        {
            return new NJobDeskBuilder(services, RegisteredSectionName(services));
        }

        services.AddSingleton(new NJobDeskSectionName(sectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<ICronService, CronosCronService>();
        services.TryAddSingleton<IExecutionHistoryStore, EmptyExecutionHistoryStore>();
        services.TryAddSingleton<ISchedulerProviderRegistry, SchedulerProviderRegistry>();
        services.TryAddSingleton<IDashboardInfoService, AggregatingDashboardInfoService>();
        services.TryAddSingleton<IDashboardManagementService, AggregatingDashboardManagementService>();

        return new NJobDeskBuilder(services, sectionName);
    }

    private static string RegisteredSectionName(IServiceCollection services) =>
        services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(NJobDeskSectionName))
            ?.ImplementationInstance is NJobDeskSectionName registered
                ? registered.Value
                : NJobDeskSectionName.Default;

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

    private sealed class CoreMarker;
}
