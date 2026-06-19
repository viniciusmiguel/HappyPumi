#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// The user who requested an update, captured at create time from the authenticated principal and carried
/// through the lifecycle into the stack's history so the console can attribute updates to the real caller.
/// </summary>
public readonly record struct UpdateActor(string Login, string Name);
