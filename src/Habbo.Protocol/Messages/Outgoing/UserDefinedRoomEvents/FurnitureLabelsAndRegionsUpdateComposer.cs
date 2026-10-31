using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>FurnitureLabelsAndRegionsUpdate (October 2026) — labels put down or changed, and the furni whose labels
/// went.</summary>
[Message(ServerMessages.FurnitureLabelsAndRegionsUpdateComposer)]
public sealed partial record FurnitureLabelsAndRegionsUpdateComposer(
    IReadOnlyList<FurnitureLabelData> AddedOrUpdated,
    IReadOnlyList<int> RemovedIds);
