using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Furniture;

/// <summary>3655 RoomDimmerPresetsMessageComposer — a dimmer's presets, which one is chosen, and whether
/// it is on. The client reads the preset count before the chosen preset, and the presets after it.</summary>
[Message(ServerMessages.RoomDimmerPresetsMessageComposer)]
public sealed partial record RoomDimmerPresetsMessageComposer(
    int SelectedPresetId,
    [property: WireCountBefore("SelectedPresetId")] IReadOnlyList<RoomDimmerPresetData> Presets,
    bool IsOn,
    int ItemId);
