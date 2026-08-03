namespace Hangfire.DemoSite;

/// <summary>Demo job targets for the seeded recurring jobs.</summary>
public static class DemoJobs
{
    public static async Task SendNewsletterDigest()
    {
        Console.WriteLine("Sending the newsletter digest...");
        await Task.Delay(TimeSpan.FromSeconds(2));
        Console.WriteLine("Newsletter digest sent.");
    }

    public static Task RefreshCache()
    {
        Console.WriteLine("Cache refreshed.");
        return Task.CompletedTask;
    }

    public static Task BuildFlakyReport() =>
        throw new InvalidOperationException("Demo failure: the report source is unavailable.");
}
