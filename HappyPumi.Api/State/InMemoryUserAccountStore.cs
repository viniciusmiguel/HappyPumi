#nullable enable

using System;
using System.Collections.Concurrent;

namespace HappyPumi.Api.State;

/// <summary>In-memory <see cref="IUserAccountStore"/> (ADR-0005), keyed by login. Used by unit tests.</summary>
public sealed class InMemoryUserAccountStore : IUserAccountStore
{
    private readonly ConcurrentDictionary<string, StoredUserAccount> _byLogin = new();

    public StoredUserAccount Get(string login)
        => _byLogin.TryGetValue(login, out var account) ? account : new StoredUserAccount { Login = login };

    public StoredUserAccount Update(string login, Action<StoredUserAccount> mutate)
    {
        var account = _byLogin.GetOrAdd(login, l => new StoredUserAccount { Login = l });
        lock (account)
            mutate(account);
        return account;
    }
}
