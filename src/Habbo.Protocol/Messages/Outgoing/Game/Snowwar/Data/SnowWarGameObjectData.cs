using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Game.Snowwar.Data;

/// <summary>A game object: its type and id, then its variables in the order the client's checksum counts them
/// (from the third on; the client knows how many from the type), and a player's name and looks.</summary>
[WireStruct]
public sealed partial record SnowWarGameObjectData(
    int Type,
    int Id,
    [property: WireCount(WireType.Uncounted)] IReadOnlyList<int> Variables,
    [property: WireWhen(nameof(SnowWarGameObjectData.IsHuman))] string Name = "",
    [property: WireWhen(nameof(SnowWarGameObjectData.IsHuman))] string Mission = "",
    [property: WireWhen(nameof(SnowWarGameObjectData.IsHuman))] string Figure = "",
    [property: WireWhen(nameof(SnowWarGameObjectData.IsHuman))] string Sex = "")
{
    public const int SnowballType = 1;
    public const int TreeType = 2;
    public const int PileType = 3;
    public const int MachineType = 4;
    public const int HumanType = 5;

    public bool IsHuman => Type == HumanType;
}
