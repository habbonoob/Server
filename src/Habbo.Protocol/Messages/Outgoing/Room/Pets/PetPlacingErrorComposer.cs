using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Pets;

/// <summary>PetPlacingErrorComposer — why a pet could not be placed</summary>
[Message(ServerMessages.PetPlacingErrorComposer)]
public sealed partial record PetPlacingErrorComposer(int ErrorCode)
{
    /// <summary>The hotel has pets switched off.</summary>
    public const int ForbiddenInHotel = 0;

    /// <summary>The room does not allow pets.</summary>
    public const int ForbiddenInFlat = 1;

    /// <summary>The room holds as many pets as it may.</summary>
    public const int MaxPets = 2;

    public const int NoFreeTiles = 3;

    /// <summary>Something stands on the tile.</summary>
    public const int TileNotFree = 4;

    /// <summary>The player has as many of their own pets in the room as they may.</summary>
    public const int MaxOwnPets = 5;
}
