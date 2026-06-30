#nullable enable

using System.Text.Json;

namespace HappyPumi.Api.Endpoints.Organizations;

/// <summary>
/// Parses the untyped <c>[FromBody] object</c> the generator binds for the project encrypt/decrypt endpoints.
/// System.Text.Json materializes that body as a <see cref="JsonElement"/>; this re-deserializes it into the
/// typed crypto contract using web defaults so base64 <c>byte[]</c> fields bind correctly. Returns null on a
/// null or malformed body (the endpoint then replies 400). Example: <c>ProjectValueBody.Parse&lt;T&gt;(req.Body)</c>.
/// </summary>
public static class ProjectValueBody
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? Parse<T>(object? body) where T : class
    {
        if (body is not JsonElement element)
            return null;
        try
        {
            return element.Deserialize<T>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
