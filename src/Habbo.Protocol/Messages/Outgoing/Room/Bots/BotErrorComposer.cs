using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>BotErrorComposer — why a bot could not be placed or renamed</summary>
[Message(ServerMessages.BotErrorComposer)]
public sealed partial record BotErrorComposer(int ErrorCode)
{
    /// <summary>The hotel has bots switched off.</summary>
    public const int ForbiddenInHotel = 0;

    /// <summary>Bots may not go in this room.</summary>
    public const int ForbiddenInFlat = 1;

    /// <summary>The room holds as many bots as it may.</summary>
    public const int LimitReached = 2;

    /// <summary>Something stands on the tile.</summary>
    public const int TileNotFree = 3;

    /// <summary>The name is not allowed.</summary>
    public const int NameNotAccepted = 4;
}
