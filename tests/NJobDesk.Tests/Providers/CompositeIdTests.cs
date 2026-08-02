using NJobDesk.Core.Providers;

namespace NJobDesk.Tests.Providers;

public class CompositeIdTests
{
    [Fact]
    public void Compose_and_split_round_trip()
    {
        var id = CompositeId.Compose("quartz", "reports.daily-sales");

        Assert.Equal("quartz:reports.daily-sales", id);
        Assert.True(CompositeId.TrySplit(id, out var providerKey, out var localId));
        Assert.Equal("quartz", providerKey);
        Assert.Equal("reports.daily-sales", localId);
    }

    [Fact]
    public void Split_uses_first_separator_when_local_id_contains_one()
    {
        Assert.True(CompositeId.TrySplit("quartz:group:name", out var providerKey, out var localId));

        Assert.Equal("quartz", providerKey);
        Assert.Equal("group:name", localId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-separator")]
    [InlineData(":leading")]
    [InlineData("trailing:")]
    public void Split_rejects_malformed_ids(string? id) =>
        Assert.False(CompositeId.TrySplit(id, out _, out _));
}
