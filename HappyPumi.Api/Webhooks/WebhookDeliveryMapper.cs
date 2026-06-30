#nullable enable

using System;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.State;

namespace HappyPumi.Api.Webhooks;

/// <summary>Maps a stored delivery to the <see cref="WebhookDelivery"/> wire shape returned by the endpoints.</summary>
public static class WebhookDeliveryMapper
{
    public static WebhookDelivery ToContract(StoredWebhookDelivery d, string requestUrl = "") => new()
    {
        Id = d.Id,
        Kind = d.Event,
        Payload = d.RequestBody,
        RequestUrl = requestUrl,
        RequestHeaders = "Content-Type: application/json",
        ResponseCode = d.ResponseStatus,
        ResponseBody = d.ResponseBody ?? "",
        ResponseHeaders = "",
        Duration = d.DurationMs,
        Timestamp = new DateTimeOffset(DateTime.SpecifyKind(d.Timestamp, DateTimeKind.Utc)).ToUnixTimeSeconds(),
    };
}
