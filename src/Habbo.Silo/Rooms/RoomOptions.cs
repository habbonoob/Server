namespace Habbo.Silo.Rooms;

/// <summary>The hotel's rules for rooms (config section "Rooms").</summary>
public sealed class RoomOptions
{
    /// <summary>The most visitors a room's settings may allow.</summary>
    public int MaxVisitorsLimit { get; set; } = 50;

    public int NameMaxLength { get; set; } = 60;

    public int DescriptionMaxLength { get; set; } = 255;

    public int MaxTags { get; set; } = 2;

    public int TagMaxLength { get; set; } = 30;

    /// <summary>Tags only staff may give a room.</summary>
    public List<string> ReservedTags { get; set; } = [];

    /// <summary>A ban's length by the name the info stand sends.</summary>
    public Dictionary<string, int> BanDurationsHours { get; set; } = [];

    /// <summary>How many rooms a player may own, without and with the club.</summary>
    public int RoomLimit { get; set; } = 25;

    public int ClubRoomLimit { get; set; } = 75;

    /// <summary>How long a visitor stays idle before falling asleep, in a room that sets no time of its own.</summary>
    public int IdleSleepSeconds { get; set; } = 300;

    /// <summary>How long a visitor stays idle before being sent out, in a room that sets no time of its own.</summary>
    public int IdleAutokickSeconds { get; set; } = 1800;

    /// <summary>The chat style a command's answer is whispered in: 1, "generic", the hotel's own bubble.</summary>
    public int CommandReplyChatStyle { get; set; } = 1;

    /// <summary>How long :mute mutes a player for when no time is given.</summary>
    public int CommandMuteMinutes { get; set; } = 10;

    /// <summary>How long a doorbell rings unanswered before the visitor is told no one came.</summary>
    public int DoorbellTimeoutSeconds { get; set; } = 60;

    /// <summary>How long a player counts as new, for the rooms kept for new players.</summary>
    public int NewPlayerDays { get; set; } = 7;

    /// <summary>The words a room's own filter may hold.</summary>
    public int FilterWordLimit { get; set; } = 100;

    /// <summary>The floor plan editor's limits: the longest side, the area without the builder's
    /// perk, and the highest the walls may be fixed at.</summary>
    public int FloorPlanMaxAxis { get; set; } = 64;

    public int FloorPlanMaxArea { get; set; } = 3025;

    public int FloorPlanMaxWallHeight { get; set; } = 16;

    /// <summary>How many ticks rollers wait between moves, when no speed controller sets it.</summary>
    public int RollerTicks { get; set; } = 4;

    /// <summary>The furni whose state sets the room's roller ticks instead.</summary>
    public string RollerSpeedControllerClass { get; set; } = string.Empty;

    /// <summary>How long a place the queue freed is kept for whoever it was freed for.</summary>
    public int QueuePlaceKeptSeconds { get; set; } = 30;

    /// <summary>The configuration furniture, by class name, and the setting each switches on.</summary>
    public Dictionary<string, ConfigurationSetting> ConfigurationItems { get; set; } = [];

    /// <summary>Who may use which hoppers, by class name; a hopper not listed is for everyone.</summary>
    public Dictionary<string, HopperRule> Hoppers { get; set; } = [];

    /// <summary>The effects, by library in the effect map, that count as a costume for a costume hopper.</summary>
    public List<string> CostumeEffects { get; set; } = [];

    /// <summary>The most rows a high score board holds; lower scores fall off.</summary>
    public int HighScoreMaxEntries { get; set; } = 50;

    /// <summary>The effects a chat command puts on (or takes off) whoever says it, by the command's name.</summary>
    public Dictionary<string, RoomChatEffect> ChatEffects { get; set; } = [];
}
