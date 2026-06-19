#nullable enable

using System.Collections.Generic;
using HappyPumi.Api.Contracts;

namespace HappyPumi.Api.Endpoints.Environments;

/// <summary>
/// Masks secret values in an evaluated property tree. Used by <c>check</c> (and friends) when the caller did
/// not request <c>showSecrets</c>: a value flagged <see cref="EscValue.Secret"/> keeps its flag but its
/// concrete value is nulled out, so secrets are not disclosed without the explicit opt-in.
/// </summary>
public static class EscRedactor
{
    public static Dictionary<string, EscValue> Mask(Dictionary<string, EscValue> properties)
    {
        foreach (var value in properties.Values)
            MaskValue(value);
        return properties;
    }

    private static void MaskValue(EscValue value)
    {
        if (value.Secret == true)
        {
            value.Value = null;
            return;
        }
        if (value.Value is Dictionary<string, EscValue> nested)
            Mask(nested);
    }
}
