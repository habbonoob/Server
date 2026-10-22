using Habbo.Domain.Pets;
using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Rooms;

/// <summary>A pet in a room: its row, who owns it, the commands it knows, and what it is doing — the
/// posture it holds and until when, whether it stays put or follows someone, and when it next wanders.</summary>
public sealed class RoomPet
{
    /// <summary>The pet as saved, with its type.</summary>
    public required Pet Data { get; init; }

    public required int OwnerAccountId { get; init; }

    public required string OwnerName { get; init; }

    /// <summary>The commands its kind can be taught, lowest level first.</summary>
    public required IReadOnlyList<PetCommand> Commands { get; init; }

    public PetType Type => Data.Type!;

    /// <summary>The posture it holds, if any, and the tick it lets go of it; 0 holds it until something else happens.</summary>
    public string? Posture { get; set; }
    public int PostureUntilTick { get; set; }

    /// <summary>Told to stay: it does not wander.</summary>
    public bool Staying { get; set; }

    /// <summary>Whom it keeps close to, if anyone.</summary>
    public Guid? Following { get; set; }

    public int NextWanderTick { get; set; }

    /// <summary>Eating, drinking or resting: it goes about nothing else until it is done.</summary>
    public bool Busy { get; set; }

    /// <summary>For a monster plant, what can be done with it as last shown: breed, harvest, revive.</summary>
    public bool CanBreed { get; set; }
    public bool CanHarvest { get; set; }
    public bool CanRevive { get; set; }

    /// <summary>The commands it obeys at its level.</summary>
    public IEnumerable<PetCommand> Obeyed => Commands.Where(c => c.RequiredLevel <= Data.Level);

    /// <summary>The pet as the room's avatar code reads any avatar: its name and look. It is nobody's
    /// account, so it has no id to send anything to.</summary>
    public PlayerData Avatar(string figure) => new(
        Guid.Empty, Data.Id, Data.Name, figure, Gender.Male, Motto: string.Empty,
        Credits: 0, SecurityLevel.None, RealName: string.Empty, DirectMail: false, RespectTotal: 0, RespectLeft: 0, PetRespectLeft: 0,
        StreamPublishingAllowed: false, NameChangeAllowed: false, AccountSafetyLocked: false, ClubLevel.None, IsAmbassador: false,
        AchievementScore: 0, IsModerator: false, LastAccess: default, HomeRoomId: PlayerData.NoHomeRoom, RespectReplenishesLeft: 0,
        TradeLocked: true, NameColor: string.Empty);
}
