namespace Habbo.Domain.Users;

/// <summary>A player account. Persistence-owned; travels between seams as read-only DTOs.</summary>
public sealed class User
{
    public Guid Id { get; set; }

    /// <summary>The wire-visible account id — Habbo protocol ids are ints.</summary>
    public int AccountId { get; set; }

    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    /// <summary>A new account's look: every set and colour one the avatar editor offers, so the figure check passes it
    /// and the player can change one part and save.</summary>
    public const string DefaultFigure = "hr-115-42.hd-190-1.ch-215-1408.lg-285-91.sh-290-1408";

    public string Figure { get; set; } = DefaultFigure;
    public Gender Gender { get; set; } = Gender.Male;
    public string Motto { get; set; } = string.Empty;
    public int Credits { get; set; }
    public SecurityLevel Rank { get; set; } = SecurityLevel.AboveNone;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>How many days in a row, up to the last login's, the user has logged in (by the hotel's UTC days).</summary>
    public int LoginStreakDays { get; set; }

    /// <summary>When the earnings window's daily gift was last given (by the hotel's UTC days).</summary>
    public DateTimeOffset? DailyGiftAt { get; set; }

    // Profile fields the client reads in UserObject / UserRights.
    public string RealName { get; set; } = string.Empty;
    public bool DirectMail { get; set; }
    public int RespectTotal { get; set; }
    public int RespectLeft { get; set; } = DailyRespects;
    public int PetRespectLeft { get; set; } = DailyRespects;

    /// <summary>The respects (and pet respects) a user gets to give each day.</summary>
    public const int DailyRespects = 3;

    /// <summary>The day the respects were last refilled; they refill on the first use of a new day.</summary>
    public DateOnly? RespectRefreshedOn { get; set; }
    public bool StreamPublishingAllowed { get; set; } = true;
    public bool NameChangeAllowed { get; set; }
    public bool AccountSafetyLocked { get; set; }
    public ClubLevel ClubLevel { get; set; }
    public bool IsAmbassador { get; set; }
    public int AchievementScore { get; set; }
    public bool IsModerator { get; set; }

    /// <summary>The room the player starts in; null when none is set.</summary>
    public int? HomeRoomId { get; set; }

    public const int EmailMaxLength = 128;

    /// <summary>The address the account is registered to; empty when none is on file.</summary>
    public string Email { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }

    /// <summary>How often the player has changed their email address; the hotel limits it.</summary>
    public int EmailChangeCount { get; set; }

    /// <summary>The player keeps their profile to themselves: others see it without its details.</summary>
    public bool ProfileHidden { get; set; }

    /// <summary>The level the profile shows.</summary>
    public int Level { get; set; }

    /// <summary>Star gems other players have given this one.</summary>
    public int StarGems { get; set; }

    /// <summary>Until when staff have muted the player hotel-wide.</summary>
    public DateTimeOffset? MutedUntil { get; set; }

    /// <summary>Until when staff have locked the player out of trading.</summary>
    public DateTimeOffset? TradeLockedUntil { get; set; }

    /// <summary>The machine the player last logged in from, as the client identifies it.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Friends may not follow the player into rooms.</summary>
    public bool FollowingBlocked { get; set; }

    /// <summary>The player takes no new friend requests.</summary>
    public bool FriendRequestsBlocked { get; set; }

    /// <summary>The group whose badge the player shows beside their name; null for none.</summary>
    public int? FavouriteGroupId { get; set; }

    /// <summary>SnowStorm games bought beyond the day's free ones.</summary>
    public int SnowWarTokens { get; set; }

    /// <summary>The day the player last bought their respects back, and how often they did that day.</summary>
    public DateOnly? RespectReplenishedOn { get; set; }

    public int RespectReplenishes { get; set; }

    /// <summary>The player may not trade (a lock staff or the account's safety set).</summary>
    public bool TradeLocked { get; set; }

    /// <summary>The colour the client draws the player's name in; empty for the usual.</summary>
    public string NameColor { get; set; } = string.Empty;
}
