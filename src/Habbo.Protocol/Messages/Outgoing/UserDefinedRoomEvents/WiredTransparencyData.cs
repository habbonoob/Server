using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents;

/// <summary>One furni (its id, a wall item's negated) or user (its room index) made see-through by wired, as the
/// visualization states carry it (the client's FurniTransparencyData and UserTransparencyData).</summary>
[WireStruct]
public sealed partial record WiredTransparencyData(int ObjectId, int Transparency, bool AllowClickThrough, int EasingMode);
