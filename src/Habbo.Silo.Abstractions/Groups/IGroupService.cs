namespace Habbo.Silo.Abstractions.Groups;

/// <summary>Groups: making, running and joining them. Every call answers the player who made it
/// itself, and tells whoever else a change concerns (the admins of a request, the members of a
/// deleted group, the rooms that show it).</summary>
public interface IGroupService
{
    Task SendCreationInfoAsync(Guid playerId);

    Task SendEditorDataAsync(Guid playerId);

    /// <summary>Buys a group; true when it was made.</summary>
    Task<bool> CreateAsync(Guid playerId, GroupDraft draft);

    Task SendEditInfoAsync(Guid playerId, int groupId);

    Task UpdateIdentityAsync(Guid playerId, int groupId, string name, string description);

    Task UpdateBadgeAsync(Guid playerId, int groupId, IReadOnlyList<int> badgeData);

    Task UpdateColorsAsync(Guid playerId, int groupId, int primaryColorId, int secondaryColorId);

    Task UpdateSettingsAsync(Guid playerId, int groupId, int guildType, int rightsLevel);

    Task DeactivateAsync(Guid playerId, int groupId);

    /// <summary>The group whose home is the room being deleted goes with it, and its members are told.</summary>
    Task RoomDeletedAsync(int roomId);

    Task JoinAsync(Guid playerId, int groupId);

    /// <summary>Takes a member out (a member leaving takes themselves out), and blocks them when asked.</summary>
    Task KickAsync(Guid playerId, int groupId, int accountId, bool block);

    Task UnblockAsync(Guid playerId, int groupId, int accountId);

    Task SetAdminAsync(Guid playerId, int groupId, int accountId, bool admin);

    Task ApproveRequestAsync(Guid playerId, int groupId, int accountId);

    Task RejectRequestAsync(Guid playerId, int groupId, int accountId);

    Task ApproveAllRequestsAsync(Guid playerId, int groupId);

    Task SendMembersAsync(Guid playerId, int groupId, int pageIndex, string userNameFilter, int searchType);

    Task SendMembershipsAsync(Guid playerId);

    Task SendDetailsAsync(Guid playerId, int groupId, bool openDetails);

    Task SendMemberFurniCountAsync(Guid playerId, int groupId, int accountId);

    /// <summary>Shows the group's badge beside the player's name; null shows none.</summary>
    Task SetFavouriteAsync(Guid playerId, int? groupId);
}
