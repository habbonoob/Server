using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>RoomDimmerPresetData</summary>
[WireStruct]
public sealed partial record RoomDimmerPresetData(int PresetId, int EffectType, string ColorHex, int Brightness);
