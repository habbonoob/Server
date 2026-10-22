using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>One grain per SSO ticket; validates and consumes it exactly once.</summary>
public interface IAuthGrain : IGrainWithStringKey
{
    Task<AuthResult> AuthenticateAsync();
}

/// <summary>Single hotel-wide registry of which players are currently online.</summary>
public interface IUserPresenceGrain : IGrainWithStringKey
{
    /// <summary>The single grain everyone's presence is tracked in.</summary>
    const string Key = "presence";

    Task AddAsync(Guid playerId);
    Task RemoveAsync(Guid playerId);
    Task<int> CountAsync();

    /// <summary>Which of these players are online. Asked as a set rather than one at a time
    /// because the messenger wants a whole friend list answered at once.</summary>
    Task<List<Guid>> WhichAreOnlineAsync(List<Guid> playerIds);

    /// <summary>Everyone online.</summary>
    Task<List<Guid>> GetOnlineAsync();
}
