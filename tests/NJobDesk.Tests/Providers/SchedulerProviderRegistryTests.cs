using NJobDesk.Core.Providers;

namespace NJobDesk.Tests.Providers;

public class SchedulerProviderRegistryTests
{
    [Fact]
    public void Preserves_registration_order_and_finds_by_key()
    {
        var first = new FakeSchedulerProvider("alpha");
        var second = new FakeSchedulerProvider("beta");

        var registry = new SchedulerProviderRegistry([first, second]);

        Assert.Equal(["alpha", "beta"], registry.Providers.Select(provider => provider.Descriptor.Key));
        Assert.Same(second, registry.Find("beta"));
        Assert.Null(registry.Find("unknown"));
    }

    [Fact]
    public void Rejects_duplicate_keys()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SchedulerProviderRegistry([new FakeSchedulerProvider("alpha"), new FakeSchedulerProvider("alpha")]));

        Assert.Contains("alpha", exception.Message);
    }

    [Theory]
    [InlineData("Has-Uppercase")]
    [InlineData("has space")]
    [InlineData("has:separator")]
    [InlineData("-leading-dash")]
    [InlineData("")]
    public void Rejects_invalid_keys(string key)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SchedulerProviderRegistry([new FakeSchedulerProvider(key)]));
    }
}
