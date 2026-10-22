namespace Habbo.Domain.Users;

/// <summary>Something a player may do because of their rank: everyone at or above <see cref="MinimumRank"/> may,
/// unless a <see cref="UserPermission"/> says otherwise for them. The names are the server's (command.ha,
/// room.manage_any, …); seed/permissions.json lists them.</summary>
public sealed class Permission
{
    public const int NameMaxLength = 64;

    public required string Name { get; set; }

    public SecurityLevel MinimumRank { get; set; }

    /// <summary>What the permission lets one do, for whoever edits the table.</summary>
    public string Description { get; set; } = string.Empty;
}
