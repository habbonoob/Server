using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>1358 ItemAddMessageComposer — one wall item, with its owner name.</summary>
[Message(ServerMessages.ItemAddMessageComposer)]
public sealed partial record ItemAddMessageComposer(WallItemData Item);
