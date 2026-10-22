using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ItemDataUpdateMessageComposer — a wall item's data: a note's colour and text</summary>
[Message(ServerMessages.ItemDataUpdateMessageComposer)]
public sealed partial record ItemDataUpdateMessageComposer([property: WireFormat(WireType.IntegerString)] int ItemId, string Data);
