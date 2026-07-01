#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Data.Entities;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Maps the persisted <see cref="ServiceRow"/> to the wire <see cref="Service"/>/<see cref="ServiceItem"/>
/// contracts. Service items round-trip through the <c>"itemType:itemName"</c> encoding held in
/// <see cref="ServiceRow.Items"/> (split on the first colon so item names may themselves contain colons).
/// </summary>
/// <example><c>ServiceMapper.ToResponse(row)</c> yields the GetService payload (service + parsed items).</example>
internal static class ServiceMapper
{
    public static GetServiceResponse ToResponse(ServiceRow row) => new()
    {
        Service = ToService(row),
        Items = ToItems(row),
        ContinuationToken = null,
    };

    public static Service ToService(ServiceRow row) => new()
    {
        Created = row.Created,
        Description = row.Description,
        Name = row.Name,
        OrganizationName = row.Org,
        ItemCountSummary = CountByType(row.Items),
        Members = new List<ServiceMember>(),
        Owner = new ServiceMember { Name = row.DisplayName, Type = "user", AvatarUrl = string.Empty },
        Properties = new List<ServiceProperty>(),
    };

    public static List<ServiceItem> ToItems(ServiceRow row)
        => row.Items.Select(encoded => ToItem(row.Org, row.Created, encoded)).ToList();

    private static ServiceItem ToItem(string org, DateTime created, string encoded)
    {
        var (type, name) = Split(encoded);
        return new ServiceItem
        {
            Type = type, Name = name, OrganizationName = org, Created = created,
            CloudCount = 0, Version = null,
        };
    }

    private static Dictionary<string, long> CountByType(IEnumerable<string> items)
        => items.GroupBy(i => Split(i).Type).ToDictionary(g => g.Key, g => (long)g.Count());

    private static (string Type, string Name) Split(string encoded)
    {
        var idx = encoded.IndexOf(':');
        if (idx < 0)
            throw new FormatException($"Service item '{encoded}' is malformed; expected 'itemType:itemName'.");
        return (encoded[..idx], encoded[(idx + 1)..]);
    }
}
