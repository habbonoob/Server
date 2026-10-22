using Habbo.Domain.Users;

namespace Habbo.Domain.Rooms;

public enum RoomState
{
    Open,
    Locked,
    Password,

    /// <summary>Kept out of listings; only those with rights see it.</summary>
    Invisible,

    /// <summary>For new players (and ambassadors and staff) only.</summary>
    NoobsOnly,
}

/// <summary>A guest room. Wire ids are ints, so the id is an identity column.</summary>
public sealed class Room
{
    public int Id { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public int ModelId { get; set; }
    public RoomModelData? Model { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public RoomState State { get; set; } = RoomState.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Visuals and settings — all client-visible values live in the room row, not in composers.
    /// <summary>The floor and wallpaper put down in the room; empty for none, as in a new room, which the client draws
    /// in its own default colours (Habbo sends a new room no floor or wallpaper at all).</summary>
    public string FloorType { get; set; } = string.Empty;
    public string Wallpaper { get; set; } = string.Empty;

    /// <summary>The landscape: a new room's is "0.0", which Habbo sends like any other.</summary>
    public string Landscape { get; set; } = NoLandscape;

    public const string NoLandscape = "0.0";
    public int MaxUsers { get; set; } = 25;
    public int WhoCanMute { get; set; }
    public int WhoCanKick { get; set; }
    public int WhoCanBan { get; set; }
    public bool AllInRoomMuted { get; set; }
    public bool IsStaffPick { get; set; }
    public int ChatMode { get; set; }
    public int ChatBubbleWidth { get; set; } = 1;
    public int ChatScrollSpeed { get; set; } = 1;
    public int ChatHearRange { get; set; } = 14;
    public int ChatFloodSensitivity { get; set; } = 1;

    // How the room treats its visitors (September 2026's room settings; the owner needs the club to change them).

    /// <summary>Whether avatars walk diagonally, or only along the tiles' edges (the owner's :diagonal).</summary>
    public bool AllowDiagonal { get; set; } = true;

    /// <summary>Walking onto the door tile takes the visitor out of the room.</summary>
    public bool LeaveOnDoorTile { get; set; } = true;

    /// <summary>A visitor idle for the timeout falls asleep.</summary>
    public bool IdleSleepEnabled { get; set; } = true;

    /// <summary>Seconds; 0 is the hotel's (RoomOptions.IdleSleepSeconds).</summary>
    public int IdleSleepTimeoutSeconds { get; set; }

    /// <summary>A visitor idle for the timeout is sent out of the room.</summary>
    public bool IdleAutokickEnabled { get; set; } = true;

    /// <summary>Seconds; 0 is the hotel's (RoomOptions.IdleAutokickSeconds).</summary>
    public int IdleAutokickTimeoutSeconds { get; set; }

    /// <summary>The room's pets keep quiet.</summary>
    public bool MuteAllPets { get; set; }

    /// <summary>The timeouts the room settings window accepts, in seconds: falling asleep, being sent out, and how
    /// long after falling asleep being sent out comes at the earliest.</summary>
    public const int IdleSleepTimeoutMin = 30, IdleSleepTimeoutMax = 3600;
    public const int IdleAutokickTimeoutMin = 60, IdleAutokickTimeoutMax = 36000;
    public const int IdleAutokickAfterSleepMin = 30;

    // Visualization + geometry settings the client reads on room entry.
    public bool WallsHidden { get; set; }
    public int WallThicknessExponent { get; set; }
    public int FloorThicknessExponent { get; set; }
    public bool UseSmallScale { get; set; }

    /// <summary>-1 lets the client derive the wall height from the heightmap.</summary>
    public int FixedWallsHeight { get; set; } = -1;

    public int CameraInitX { get; set; }
    public int CameraInitY { get; set; }
    public double CameraInitZ { get; set; }

    /// <summary>The controller level the client grants a room owner.</summary>
    public int OwnerControllerLevel { get; set; } = 4;

    /// <summary>The most floor and wall furniture the room holds, as Habbo's wired monitor reports
    /// a room's (3872 each).</summary>
    public const int DefaultItemLimit = 3872;

    public int FloorItemLimit { get; set; } = DefaultItemLimit;
    public int WallItemLimit { get; set; } = DefaultItemLimit;

    /// <summary>Who besides the owner may change, and read, the room's wired: a bit per
    /// permission level (everyone, users with rights, group members, group admins). Habbo starts
    /// a room at its group admins (8) for both.</summary>
    public const int DefaultWiredPermissionMask = 1 << 3;

    public int WiredModifyPermissionMask { get; set; } = DefaultWiredPermissionMask;
    public int WiredReadPermissionMask { get; set; } = DefaultWiredPermissionMask;

    /// <summary>The time zone the room's wired tells time in; empty for the hotel's.</summary>
    public string WiredTimezone { get; set; } = string.Empty;

    public const int WiredTimezoneMaxLength = 64;

    // Navigator listing fields.
    public int TradeMode { get; set; }
    public int Score { get; set; }
    public int Ranking { get; set; }

    /// <summary>-1 is the client's "uncategorised".</summary>
    public int CategoryId { get; set; } = -1;

    public List<string> Tags { get; set; } = [];

    public bool ShowOwner { get; set; } = true;
    public bool AllowPets { get; set; }

    public const int PasswordMaxLength = 64;

    /// <summary>What a password-locked room asks for.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Visitors may eat others' pets' food.</summary>
    public bool AllowFoodConsume { get; set; }

    /// <summary>Avatars walk through each other.</summary>
    public bool AllowWalkThrough { get; set; }

    /// <summary>The navigator may list the room under its dynamic categories.</summary>
    public bool AllowNavigatorDynamicCategories { get; set; } = true;

    /// <summary>A builders club room hidden while its builder's membership has lapsed.</summary>
    public bool HiddenByBuildersClub { get; set; }

    /// <summary>How many may wait in line outside when the room is full; none, and a full room is full.</summary>
    public int QueueLimit { get; set; }

    /// <summary>How many may watch without an avatar; none, and the room has no stands.</summary>
    public int SpectatorLimit { get; set; }

    /// <summary>Image shown for official rooms; null for guest rooms.</summary>
    public string? OfficialPicRef { get; set; }

    // The thumbnail the owner composed in the navigator: background, the room's picture, and an overlay.
    public int ThumbnailBackgroundImageId { get; set; }
    public int ThumbnailFrontImageId { get; set; }
    public int ThumbnailForegroundImageId { get; set; }
}
