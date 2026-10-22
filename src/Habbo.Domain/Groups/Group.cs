using Habbo.Domain.Rooms;
using Habbo.Domain.Users;

namespace Habbo.Domain.Groups;

/// <summary>A group (the client says guild): a named club with a badge, colours and a home room,
/// whose members wear its badge and may decorate its room.</summary>
public sealed class Group
{
    public const int NameMaxLength = 30;
    public const int DescriptionMaxLength = 255;
    public const int BadgeCodeMaxLength = 64;

    public int Id { get; set; }

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>The badge the imager draws, built from <see cref="BadgeLayers"/>.</summary>
    public string BadgeCode { get; set; } = string.Empty;

    /// <summary>The group's home room; a room is home to one group at most.</summary>
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public int PrimaryColorId { get; set; }
    public int SecondaryColorId { get; set; }

    public GroupState State { get; set; } = GroupState.Open;

    /// <summary>Whether members get rights in the home room (the client's rights level 0).</summary>
    public bool MembersCanDecorate { get; set; }

    /// <summary>Staff have frozen the group's identity; its admins cannot edit it.</summary>
    public bool Locked { get; set; }

    /// <summary>A link the group shows, set by staff.</summary>
    public string Url { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<GroupBadgeLayer> BadgeLayers { get; set; } = [];

    // The forum, which a group gets by buying one.
    public bool HasForum { get; set; }
    public GroupForumPermission ForumReadPermission { get; set; } = GroupForumPermission.Everyone;
    public GroupForumPermission ForumPostMessagePermission { get; set; } = GroupForumPermission.Members;
    public GroupForumPermission ForumPostThreadPermission { get; set; } = GroupForumPermission.Members;
    public GroupForumPermission ForumModeratePermission { get; set; } = GroupForumPermission.Admins;

    /// <summary>Staff have suspended the forum.</summary>
    public bool ForumDisabled { get; set; }

    /// <summary>How often the forum was opened; the "most viewed" list sorts by it.</summary>
    public int ForumViews { get; set; }
}
