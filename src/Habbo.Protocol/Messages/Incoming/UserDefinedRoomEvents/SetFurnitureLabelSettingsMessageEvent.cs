using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

/// <summary>SetFurnitureLabelSettings (October 2026) — a wired label's bubble colour (0xRRGGBB), who sees it (0
/// everyone, 1 those who may read the room's wired) and whether it stays open.</summary>
[Incoming(ClientMessages.SetFurnitureLabelSettingsMessageEvent)]
public sealed partial record SetFurnitureLabelSettingsMessageEvent(
    int ItemId,
    int BubbleColor,
    int Visibility,
    bool AlwaysOpen);
