#nullable enable

using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Maps a persisted <see cref="StoredCmk"/> to the wire <see cref="CustomerManagedKey"/>. State is derived:
/// a disabled key is <c>disabled</c>, the org's default is <c>default</c>, any other enabled key is
/// <c>active</c>. Example: <c>OrganizationKeyMapper.ToContract(key)</c>.
/// </summary>
public static class OrganizationKeyMapper
{
    public static CustomerManagedKey ToContract(StoredCmk key) => new()
    {
        Id = key.Id,
        KeyType = key.KeyType,
        Name = key.Name,
        State = State(key),
        AwsKms = key.KeyArn is null ? null : new AwsKmsConfig { KeyArn = key.KeyArn, RoleArn = key.RoleArn ?? "" },
    };

    private static string State(StoredCmk key)
    {
        if (!key.Enabled)
            return "disabled";
        return key.IsDefault ? "default" : "active";
    }
}
