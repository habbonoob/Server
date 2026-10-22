using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Subscriptions;

/// <summary>Reads a player's subscriptions, and the limits a Builders Club tier carries.</summary>
public interface ISubscriptionService
{
    /// <summary>Where one product stands for a player. Never null: a player who never joined
    /// still gets a report saying so, which is what the client expects at login.</summary>
    Task<SubscriptionView> GetAsync(Guid playerId, string productName, CancellationToken ct = default);

    Task<BuildersClubView> GetBuildersClubAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>The Builders Club furni the player has out, which their club limit counts.</summary>
    Task<int> CountBuildersClubFurniAsync(Guid playerId, CancellationToken ct = default);

    /// <summary>Shows the player's rooms that building on the trial hid in the navigator again, now that they have
    /// membership time; how many there were.</summary>
    Task<int> UnhideBuildersClubRoomsAsync(Guid playerId, CancellationToken ct = default);
}
