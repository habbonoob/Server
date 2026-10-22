using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>2588 ItemUpdateMessageComposer.</summary>
[Message(ServerMessages.ItemUpdateMessageComposer)]
public sealed partial record ItemUpdateMessageComposer(WallItemData Item);
