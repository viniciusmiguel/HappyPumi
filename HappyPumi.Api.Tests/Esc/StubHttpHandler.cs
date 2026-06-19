using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> that captures each request (method, URI, body) and replies with a
/// caller-supplied JSON body, so the OIDC exchangers' HTTP shaping can be asserted without a network call.
/// Responses are returned in the order configured (the last one repeats once exhausted), letting multi-step
/// exchanges like GCP's STS-then-impersonate be stubbed.
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly List<string> _responses = new();
    private readonly HttpStatusCode _status;

    public StubHttpHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Add(responseJson);
        _status = status;
    }

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastBody { get; private set; }
    public int CallCount { get; private set; }

    /// <summary>Queues the body returned by the next call (for multi-step exchanges).</summary>
    public StubHttpHandler ThenRespondWith(string responseJson)
    {
        _responses.Add(responseJson);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        var body = _responses[System.Math.Min(CallCount, _responses.Count - 1)];
        CallCount++;
        return new HttpResponseMessage(_status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
