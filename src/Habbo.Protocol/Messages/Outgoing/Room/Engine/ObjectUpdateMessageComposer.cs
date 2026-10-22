using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>3025 ObjectUpdateMessageComposer — same layout, no owner name.</summary>
[Message(ServerMessages.ObjectUpdateMessageComposer)]
public sealed partial record ObjectUpdateMessageComposer(FloorItemData Item);
