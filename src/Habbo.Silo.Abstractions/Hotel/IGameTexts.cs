using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>Resolves the gesture an emoticon in chat should play. The emoticon list is data.</summary>
/// <summary>The game texts the client is served, for what the client names only there.</summary>
public interface IGameTexts
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
}
