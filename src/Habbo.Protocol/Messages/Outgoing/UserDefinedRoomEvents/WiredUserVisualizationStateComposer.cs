using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>WiredUserVisualizationState (October 2026) — how wired left the room's users looking, by room index, for one
/// coming in; as <see cref="WiredFurniVisualizationStateComposer"/>.</summary>
[Message(ServerMessages.WiredUserVisualizationStateComposer)]
public sealed partial record WiredUserVisualizationStateComposer(
    IReadOnlyList<WiredTransparencyData> Transparencies, IReadOnlyList<WiredHighlightData> Highlights);
