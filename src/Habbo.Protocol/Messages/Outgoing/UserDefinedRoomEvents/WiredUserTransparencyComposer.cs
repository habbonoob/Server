using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>WiredUserTransparency (October 2026) — wired's set visibility made users see-through, by room index; as
/// <see cref="WiredFurniTransparencyComposer"/>.</summary>
[Message(ServerMessages.WiredUserTransparencyComposer)]
public sealed partial record WiredUserTransparencyComposer(
    IReadOnlyList<int> RoomIndices, int Transparency, bool AllowClickThrough, int EasingMode);
