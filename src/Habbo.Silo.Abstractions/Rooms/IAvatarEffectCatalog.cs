using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>Habbo's avatar effects, for the room to find the ones it puts on avatars itself.</summary>
public interface IAvatarEffectCatalog
{
    /// <summary>The id of the effect with this library name ("WiredTeamRed"), if there is one.</summary>
    Task<int?> FindAsync(string library, CancellationToken ct = default);

    Task<bool> ExistsAsync(int effectId, CancellationToken ct = default);
}
