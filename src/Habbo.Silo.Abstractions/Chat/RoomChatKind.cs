namespace Habbo.Silo.Abstractions.Chat;

/// <summary>How something is said in a room, numbered as the client numbers its chat types.</summary>
public enum RoomChatKind
{
    /// <summary>Heard by those within the room's hearing distance.</summary>
    Talk = 0,

    /// <summary>Heard only by the one it is for.</summary>
    Whisper = 1,

    /// <summary>Heard by the whole room.</summary>
    Shout = 2,
}
