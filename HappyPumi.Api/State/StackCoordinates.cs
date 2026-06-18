#nullable enable

namespace HappyPumi.Api.State;

/// <summary>
/// The three-part identity of a stack as it appears in every <c>/api/stacks/{orgName}/{projectName}/{stackName}</c>
/// route. Used as the key into <see cref="IStackStore"/>.
/// </summary>
/// <example><code>var coords = new StackCoordinates("happypumi", "webapp", "dev");</code></example>
public readonly record struct StackCoordinates(string Org, string Project, string Stack)
{
    /// <summary>The fully-qualified stack name (<c>org/project/stack</c>) used as the logical stack id.</summary>
    public string Qualified => $"{Org}/{Project}/{Stack}";
}
