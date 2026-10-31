using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>FurniRecolorUpdate (October 2026) — recolourable furni recoloured: the client writes each update's colour
/// into its furni's r, g and b data, and the recolour window takes the one it asked for as its answer.</summary>
[Message(ServerMessages.FurniRecolorUpdateMessageComposer)]
public sealed partial record FurniRecolorUpdateMessageComposer(IReadOnlyList<RecolorUpdateData> Updates);

/// <summary>Furni given one colour, a channel each.</summary>
[WireStruct]
public sealed partial record RecolorUpdateData(
    IReadOnlyList<int> FurniIds,
    [property: WireFormat(WireType.I16)] short Red,
    [property: WireFormat(WireType.I16)] short Green,
    [property: WireFormat(WireType.I16)] short Blue);
