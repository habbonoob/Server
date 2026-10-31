using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>FurnitureLabelsAndRegions (October 2026) — every label in the room, sent on entry after the wired
/// environment (Habbo's own room entry, captured). The client shows each to those its visibility allows.</summary>
[Message(ServerMessages.FurnitureLabelsAndRegionsComposer)]
public sealed partial record FurnitureLabelsAndRegionsComposer(
    IReadOnlyList<FurnitureLabelData> Entries);
