#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace HappyPumi.Api.State;

/// <summary>
/// Applies the optional <c>filter</c> query parameter to a token list: a case-insensitive substring match on
/// name or description (the personal/org/team list endpoints share this). A null/empty filter is a no-op.
/// </summary>
public static class AccessTokenFilter
{
    public static IReadOnlyList<StoredAccessToken> Apply(IReadOnlyList<StoredAccessToken> tokens, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return tokens;
        return tokens.Where(t =>
            t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
