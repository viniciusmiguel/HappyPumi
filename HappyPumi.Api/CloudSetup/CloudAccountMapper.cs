#nullable enable

using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.CloudSetup;

/// <summary>
/// Projects between the stored <see cref="CloudAccountEntry"/> and the wire <see cref="CloudAccount"/>
/// contract for the cloud-setup endpoints (PR6). Keeps the persistence model free of contract types.
/// </summary>
public static class CloudAccountMapper
{
    public static ListCloudAccountsResponse ToResponse(IReadOnlyList<CloudAccountEntry> accounts)
        => new() { Accounts = accounts.Select(ToContract).ToList() };

    public static CloudAccount ToContract(CloudAccountEntry e)
        => new() { Id = e.Id, Name = e.Name, Number = e.Number, Roles = e.Roles };

    public static CloudAccountEntry ToEntry(CloudAccount a)
        => new() { Id = a.Id, Name = a.Name, Number = a.Number, Roles = a.Roles };
}
