using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>WiredFurniTransparency (October 2026) — wired's set visibility made furni see-through: their ids (a wall
/// item's negated), how transparent (0 visible, 100 gone), whether clicks pass through them, and how the change eases in.</summary>
[Message(ServerMessages.WiredFurniTransparencyComposer)]
public sealed partial record WiredFurniTransparencyComposer(
    IReadOnlyList<int> FurniIds, int Transparency, bool AllowClickThrough, int EasingMode);
