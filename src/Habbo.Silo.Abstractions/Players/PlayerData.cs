using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Players;

/// <summary>Read-only view of a player account; safe to pass between seams and grains.
/// Carries every field the client-facing packets need — writers never invent values.</summary>
[GenerateSerializer]
public partial record PlayerData(
    [property: Id(0)] Guid Id,
    [property: Id(1)] int AccountId,
    [property: Id(2)] string Username,
    [property: Id(3)] string Figure,
    [property: Id(4)] Gender Gender,
    [property: Id(5)] string Motto,
    [property: Id(6)] int Credits,
    [property: Id(7)] SecurityLevel Rank,
    [property: Id(8)] string RealName,
    [property: Id(9)] bool DirectMail,
    [property: Id(10)] int RespectTotal,
    [property: Id(11)] int RespectLeft,
    [property: Id(12)] int PetRespectLeft,
    [property: Id(13)] bool StreamPublishingAllowed,
    [property: Id(14)] bool NameChangeAllowed,
    [property: Id(15)] bool AccountSafetyLocked,
    [property: Id(16)] ClubLevel ClubLevel,
    [property: Id(17)] bool IsAmbassador,
    [property: Id(18)] int AchievementScore,
    [property: Id(19)] bool IsModerator,
    [property: Id(20)] DateTimeOffset LastAccess,
    [property: Id(21)] int HomeRoomId,
    [property: Id(22)] int RespectReplenishesLeft,
    [property: Id(23)] bool TradeLocked,
    [property: Id(24)] string NameColor,
    /// <summary>Until when staff have muted the player, if they have.</summary>
    [property: Id(25)] DateTimeOffset? MutedUntil = null,
    /// <summary>When the account was made.</summary>
    [property: Id(26)] DateTimeOffset CreatedAt = default)
{
    /// <summary>The client's "no home room" id.</summary>
    public const int NoHomeRoom = 0;
}

/// <summary>Outcome of validating an SSO ticket.</summary>
[GenerateSerializer]
public partial record AuthResult(
    [property: Id(0)] bool Success,
    [property: Id(1)] PlayerData? Player,
    [property: Id(2)] string? Error,
    /// <summary>When the player last logged in before this login, or null when this is their
    /// first ever. It is a fact about the login rather than about the account, which is why it
    /// rides here: by the time the player data is built, the current login has already replaced
    /// it on the account.</summary>
    [property: Id(3)] DateTimeOffset? PreviousLoginAt = null)
{
    public static AuthResult Ok(PlayerData player, DateTimeOffset? previousLoginAt) =>
        new(true, player, null, previousLoginAt);

    public static AuthResult Failed(string error) => new(false, null, error);
}
