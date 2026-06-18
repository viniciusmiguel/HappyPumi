#nullable enable

namespace HappyPumi.Api.State;

/// <summary>The registry identity of a package: <c>source/publisher/name</c> (versions hang off this).</summary>
public readonly record struct PackageCoordinates(string Source, string Publisher, string Name);
