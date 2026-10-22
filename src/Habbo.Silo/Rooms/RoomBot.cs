using Habbo.Domain.Bots;
using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Rooms;

/// <summary>A bot standing in a room: its row, who owns it, and when it next talks or walks.</summary>
public sealed class RoomBot
{
    /// <summary>The bot as saved, with its type.</summary>
    public required Bot Data { get; init; }

    public required int OwnerAccountId { get; init; }

    public required string OwnerName { get; init; }

    /// <summary>The tick it next says something by itself, or walks somewhere.</summary>
    public int NextChatTick { get; set; }
    public int NextWalkTick { get; set; }

    /// <summary>The user wired set the bot following, and where they stood when it last set off after them.</summary>
    public RoomEntityStore.RoomEntity? Following { get; set; }
    public (int X, int Y)? FollowedAt { get; set; }

    public BotType Type => Data.Type!;

    public bool Has(int skill) => Type.Skills.Contains(skill);

    public IReadOnlyList<short> Skills => Type.Skills.Select(skill => (short)skill).ToList();

    /// <summary>The bot as the room's avatar code reads any avatar: its name, look and motto. It is
    /// nobody's account, so it has no id to send anything to.</summary>
    public PlayerData Avatar() => new(
        Guid.Empty, Data.Id, Data.Name, Data.Figure, Data.Gender, Data.Motto,
        Credits: 0, SecurityLevel.None, RealName: string.Empty, DirectMail: false, RespectTotal: 0, RespectLeft: 0, PetRespectLeft: 0,
        StreamPublishingAllowed: false, NameChangeAllowed: false, AccountSafetyLocked: false, ClubLevel.None, IsAmbassador: false,
        AchievementScore: 0, IsModerator: false, LastAccess: default, HomeRoomId: PlayerData.NoHomeRoom, RespectReplenishesLeft: 0,
        TradeLocked: true, NameColor: string.Empty);
}
