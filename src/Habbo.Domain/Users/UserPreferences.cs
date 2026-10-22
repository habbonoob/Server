namespace Habbo.Domain.Users;

/// <summary>A player's client settings, as AccountPreferencesComposer carries them. One row per
/// user, created with these defaults the first time it's needed.</summary>
public sealed class UserPreferences
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int UiVolume { get; set; } = 100;
    public int FurniVolume { get; set; } = 100;
    public int TraxVolume { get; set; } = 100;

    /// <summary>True when the player chose the old (non-free-flow) chat.</summary>
    public bool FreeFlowChatDisabled { get; set; }

    /// <summary>Whether the client draws the info feed, the strip of hotel notices beside the
    /// chat. On by default, as the client's own setting is.</summary>
    public bool InfoFeedEnabled { get; set; } = true;

    public bool RoomInvitesIgnored { get; set; }
    public bool RoomCameraFollowDisabled { get; set; }
    public int UiFlags { get; set; }

    /// <summary>Style 0 is the classic white bubble; the client's own default (1) is the
    /// dark console bubble.</summary>
    public int PreferedChatStyle { get; set; }

    /// <summary>The chat's text size: 0 is the client's own, 1-4 each a step larger.</summary>
    public int ChatFontSize { get; set; }

    /// <summary>How rooms' chat scrolls for this player: 0 free flow, 1 line by line. The room decided this until
    /// September 2026; the player does since.</summary>
    public int ChatMode { get; set; }

    /// <summary>Chat bubble width: 0 narrow, 1 normal, 2 wide.</summary>
    public int ChatBubbleWidth { get; set; } = 1;

    /// <summary>How fast the chat scrolls up: 0 slow, 1 normal, 2 fast.</summary>
    public int ChatScrollSpeed { get; set; } = 1;

    /// <summary>Who sees the player online.</summary>
    public OnlineIndicatorPreference OnlineIndicator { get; set; }

    public bool WiredMenuButton { get; set; }
    public bool WiredInspectButton { get; set; }
    public bool PlayTestMode { get; set; }
    public bool WiredWhisperDisabled { get; set; }
    public bool ShowAllNotifications { get; set; } = true;

    /// <summary>How the player shows on Discord: the client's preferences version, then each choice.</summary>
    public int DiscordVersion { get; set; }
    public bool DiscordShowHabbo { get; set; }
    public bool DiscordShareActivity { get; set; }
    public bool DiscordHideInHiddenRooms { get; set; }
    public bool DiscordAllowJoining { get; set; }

    /// <summary>When the player last opened the catalog's new additions.</summary>
    public DateTimeOffset? CatalogNewAdditionsSeenAt { get; set; }
    public string WiredUiStyle { get; set; } = string.Empty;

    /// <summary>A style is a name from the client's list; nothing longer is one.</summary>
    public const int WiredUiStyleMaxLength = 64;

    /// <summary>The client's choices for each chat setting, 0 up to these: what it would turn anything else into its
    /// default, so anything else is not a choice.</summary>
    public const int ChatFontSizeMax = 4;
    public const int ChatModeMax = 1;
    public const int ChatBubbleWidthMax = 2;
    public const int ChatScrollSpeedMax = 2;
}
