using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>WiredFurniVisualizationState (October 2026) — how wired left the room's furni looking, for one coming in:
/// shown at once, not eased in.</summary>
[Message(ServerMessages.WiredFurniVisualizationStateComposer)]
public sealed partial record WiredFurniVisualizationStateComposer(
    IReadOnlyList<WiredTransparencyData> Transparencies, IReadOnlyList<WiredHighlightData> Highlights);
