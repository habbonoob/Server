using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>One entry of <see cref="WiredMovementsMessageComposer"/>: its kind, then that kind's
/// payload. Each kind of movement converts to one.</summary>
[WireStruct]
public sealed partial record WiredMovementData(
    int Kind,
    [property: WireWhen("IsUserMove")] WiredUserMoveData? UserMove,
    [property: WireWhen("IsFurniMove")] WiredFurniMoveData? FurniMove,
    [property: WireWhen("IsUserDirection")] WiredUserDirectionData? UserDirection)
{
    // the kinds of movement the client knows
    public const int UserMoveKind = 0;
    public const int FurniMoveKind = 1;
    public const int WallItemMoveKind = 2;
    public const int UserDirectionKind = 3;

    public bool IsUserMove => Kind == UserMoveKind;
    public bool IsFurniMove => Kind == FurniMoveKind;
    public bool IsUserDirection => Kind == UserDirectionKind;

    public static implicit operator WiredMovementData(WiredUserMoveData move) => new(UserMoveKind, move, null, null);

    public static implicit operator WiredMovementData(WiredFurniMoveData move) => new(FurniMoveKind, null, move, null);

    public static implicit operator WiredMovementData(WiredUserDirectionData turn) => new(UserDirectionKind, null, null, turn);
}
