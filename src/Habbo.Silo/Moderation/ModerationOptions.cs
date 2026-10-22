using Habbo.Domain.Users;

namespace Habbo.Silo.Moderation;

/// <summary>The hotel's rules for moderation (config section "Moderation").</summary>
public sealed class ModerationOptions
{
    /// <summary>Whether players may send calls for help at all.</summary>
    public bool CallForHelpEnabled { get; set; } = true;

    /// <summary>Where the client points a player when calls for help are off.</summary>
    public string CallForHelpInfoUrl { get; set; } = string.Empty;

    /// <summary>How long a player whose call was abusive waits before they may call again.</summary>
    public int AbusiveCallCooldownMinutes { get; set; } = 10;

    /// <summary>How many calls for help one player may make an hour, deleted ones counted.</summary>
    public int CallsPerHour { get; set; } = 5;

    /// <summary>Lines per chat log.</summary>
    public int ChatlogLines { get; set; } = 150;

    /// <summary>How many of a user's last rooms their chat log covers.</summary>
    public int UserChatlogRooms { get; set; } = 5;

    public int RoomVisitsLimit { get; set; } = 50;

    /// <summary>What a room taken in hand by staff is renamed to.</summary>
    public string ModeratedRoomName { get; set; } = "Inappropriate to Hotel Management";

    /// <summary>The date and time format of the chat logs and reports.</summary>
    public string TimestampFormat { get; set; } = "HH:mm";
}
