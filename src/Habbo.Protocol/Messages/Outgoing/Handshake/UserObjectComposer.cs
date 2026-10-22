using Habbo.Domain.Users;
using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>1338 UserObjectComposer — the 14 mandatory fields, then the trade lock and name colour,
/// then the respect refills, which the client reads when present.</summary>
[Message(ServerMessages.UserObjectComposer)]
public sealed partial record UserObjectComposer(
    int AccountId,
    string Username,
    string Figure,
    string Sex,
    string CustomData,
    string RealName,
    bool DirectMail,
    int RespectTotal,
    int RespectLeft,
    int PetRespectLeft,
    bool StreamPublishingAllowed,
    string LastAccess,
    bool NameChangeAllowed,
    bool AccountSafetyLocked,
    bool AccountTradeLocked,
    string NameColor,
    int RespectReplenishesLeft,
    int MaxRespectPerDay)
{
    /// <summary>The date format the client parses the last-access timestamp with.</summary>
    private const string LastAccessFormat = "yyyy-MM-dd HH:mm:ss";

    public static UserObjectComposer From(PlayerData player, DateTimeOffset lastAccess) => new(
        player.AccountId,
        player.Username,
        player.Figure,
        player.Gender.ToLetter(),
        player.Motto,
        player.RealName,
        player.DirectMail,
        player.RespectTotal,
        player.RespectLeft,
        player.PetRespectLeft,
        player.StreamPublishingAllowed,
        lastAccess.ToString(LastAccessFormat),
        player.NameChangeAllowed,
        player.AccountSafetyLocked,
        player.TradeLocked,
        player.NameColor,
        player.RespectReplenishesLeft,
        User.DailyRespects);
}
