#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// The update status strings the CLI understands (pulumi apitype.UpdateStatus). CompleteUpdate sends
/// one of <see cref="Succeeded"/> / <see cref="Failed"/>; the others are server-side lifecycle states.
/// </summary>
public static class UpdateStatuses
{
    public const string NotStarted = "not started";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
}
