using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>A game event: its type, then its arguments, all numbers, as many as the client reads for the type.</summary>
[WireStruct]
public sealed partial record SnowWarGameEventData(
    int Type,
    [property: WireCount(WireType.Uncounted)] IReadOnlyList<int> Arguments)
{
    public const int HumanLeftGame = 1;
    public const int NewMoveTarget = 2;
    public const int HumanThrowsSnowballAtHuman = 3;
    public const int HumanThrowsSnowballAtPosition = 4;
    public const int HumanStartsToMakeASnowball = 7;
    public const int CreateSnowball = 8;
    public const int MachineCreatesSnowball = 11;
    public const int HumanGetsSnowballsFromMachine = 12;
}
