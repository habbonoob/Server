namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>Whether a player may come into a room, and if not, why.</summary>
public enum RoomAccess
{
    Allowed,
    NoSuchRoom,
    Full,
    Banned,
    WrongPassword,

    /// <summary>The doorbell rang: the player waits for somebody inside to open.</summary>
    Ringing,

    /// <summary>Nobody inside can open the door.</summary>
    NobodyToOpen,

    /// <summary>A room for new players, and the player is not one.</summary>
    NotANewPlayer,

    /// <summary>The room is full and the player waits in its line.</summary>
    Queued,

    /// <summary>The player comes in to watch, without an avatar.</summary>
    Spectator,

    /// <summary>The room holds Builders Club furni and its owner has no membership time left: it takes no visitors.</summary>
    LockedByBuildersClub,
}
