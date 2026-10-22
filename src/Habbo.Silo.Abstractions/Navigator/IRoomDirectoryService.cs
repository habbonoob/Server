using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>The old navigator and what surrounds finding a room: its searches, the official rooms
/// view, forwards to named rooms and networks, room creation limits, staff picks and thumbnails.
/// Every call answers the player who made it.</summary>
public interface IRoomDirectoryService
{
    /// <summary>Runs one of the old navigator's searches. <paramref name="adIndex"/> picks which
    /// ad takes the result's ad slot; searches without one show none.</summary>
    Task SearchAsync(PlayerData player, LegacyRoomSearch search, string param, int? adIndex);

    Task SearchCompetitionAsync(PlayerData player, int goalId, int pageIndex);

    Task SendOfficialRoomsAsync(PlayerData player, int adIndex);

    Task SendCategoriesWithUserCountAsync(PlayerData player);

    /// <summary>A room link from outside the client: the room it names.</summary>
    Task ConvertGlobalRoomIdAsync(PlayerData player, string globalId);

    /// <summary>A named room ("navigator/goto/predefined_noob_lobby").</summary>
    Task ForwardToSomeRoomAsync(PlayerData player, string type);

    /// <summary>A random room of a promoted category ("partyRoom").</summary>
    Task ForwardToRandomPromotedRoomAsync(PlayerData player, string categoryCode);

    Task OpenRoomNetworkAsync(PlayerData player, int networkId, int homeRoomId);

    Task SendCanCreateRoomAsync(PlayerData player);

    /// <summary>Whether the player may make another room; when not, they are told their limit.</summary>
    Task<bool> EnsureCanCreateRoomAsync(PlayerData player);

    Task ToggleStaffPickAsync(PlayerData player, int roomId, bool staffPick);

    Task UpdateThumbnailAsync(PlayerData player, int roomId, int backgroundImageId, int frontImageId, int foregroundImageId);

    Task RecordPromotionsTabViewedAsync(PlayerData player);

    Task RecordPromotionClickedAsync(PlayerData player, int roomId);
}
