namespace NJobDesk.Core.Models;

public record DashboardStatusModel
{
    public bool ReadOnly { get; init; }

    public IReadOnlyList<ProviderStatusModel> Providers { get; init; } = [];
}
