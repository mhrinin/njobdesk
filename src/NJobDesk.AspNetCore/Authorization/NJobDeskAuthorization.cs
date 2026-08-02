namespace NJobDesk.AspNetCore.Authorization;

public static class NJobDeskAuthorization
{
    /// <summary>
    /// The authorization policy guarding every dashboard API and UI endpoint. Define it in the host
    /// to control access; when left undefined, a fallback policy allows local requests only.
    /// </summary>
    public const string PolicyName = "NJobDesk";
}
