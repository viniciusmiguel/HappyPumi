#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HappyPumi.Api.Contracts;
using HappyPumi.Api.Esc.Providers.AwsSecrets;

namespace HappyPumi.Api.Esc.Rotators.AwsIam;

/// <summary>
/// The <c>fn::rotate::aws-iam</c> rotator: rotates an AWS IAM user's access key. On rotation it creates a new
/// key for the user and deletes the previously-current one (if any), returning the new credentials.
/// Inputs (already interpolated):
/// <code>
/// region: us-east-1
/// user: my-bot            # IAM user name (or userArn: arn:aws:iam::123:user/my-bot)
/// login: { accessKeyId, secretAccessKey, sessionToken }   # optional admin credentials; else the default chain
/// </code>
/// Output (the new <c>state.current</c>): <c>{ accessKeyId: {fn::secret}, secretAccessKey: {fn::secret} }</c>.
/// </summary>
public sealed class AwsIamRotator(IAwsIamClient client) : IEscRotator
{
    public string Name => "aws-iam";

    public string Description => "Rotates an AWS IAM user's access key via fn::rotate::aws-iam.";

    public EscSchemaSchema Inputs => new()
    {
        Type = "object",
        Required = new List<string> { "region" },
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["region"] = new() { Type = "string", Description = "AWS region, e.g. us-east-1." },
            ["user"] = new() { Type = "string", Description = "IAM user name (or supply userArn)." },
            ["userArn"] = new() { Type = "string", Description = "IAM user ARN; the user name is taken from its suffix." },
            ["login"] = new() { Type = "object", Description = "Optional admin credentials; the default chain otherwise." },
        },
    };

    public EscSchemaSchema Outputs => new()
    {
        Type = "object",
        Description = "The rotated access key.",
        Properties = new Dictionary<string, EscSchemaSchema>
        {
            ["accessKeyId"] = new() { Type = "string", Secret = true },
            ["secretAccessKey"] = new() { Type = "string", Secret = true },
        },
    };

    public async Task<object?> RotateAsync(
        IReadOnlyDictionary<string, object?> inputs,
        IReadOnlyDictionary<string, object?>? currentState,
        CancellationToken ct)
    {
        var region = EscProviderInputs.Require<string>(inputs, Name, "region");
        var userName = UserName(inputs);
        var login = ParseLogin(inputs);

        var newKey = await client.CreateAccessKeyAsync(region, userName, login, ct);
        if (currentState?.GetValueOrDefault("accessKeyId") is string previous && !string.IsNullOrWhiteSpace(previous))
            await client.DeleteAccessKeyAsync(region, userName, previous, login, ct);

        return new Dictionary<string, object?>
        {
            ["accessKeyId"] = EscProviderInputs.Secret(newKey.AccessKeyId),
            ["secretAccessKey"] = EscProviderInputs.Secret(newKey.SecretAccessKey),
        };
    }

    // The user name is given directly, or taken from the ARN suffix after "user/".
    private static string UserName(IReadOnlyDictionary<string, object?> inputs)
    {
        if (inputs.GetValueOrDefault("user") is string user && !string.IsNullOrWhiteSpace(user))
            return user;
        if (inputs.GetValueOrDefault("userArn") is string arn && arn.Contains('/'))
            return arn[(arn.LastIndexOf('/') + 1)..];
        throw new ArgumentException("aws-iam requires 'user' or 'userArn'.");
    }

    private static AwsLogin? ParseLogin(IReadOnlyDictionary<string, object?> inputs)
    {
        if (inputs.GetValueOrDefault("login") is not Dictionary<string, object?> login)
            return null;
        return new AwsLogin(
            login.GetValueOrDefault("accessKeyId") as string,
            login.GetValueOrDefault("secretAccessKey") as string,
            login.GetValueOrDefault("sessionToken") as string);
    }
}
