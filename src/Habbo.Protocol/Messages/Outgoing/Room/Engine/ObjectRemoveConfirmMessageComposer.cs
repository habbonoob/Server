using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>ObjectRemoveConfirmMessageComposer — a pickup that needs saying yes to first: the item (type 1 wall, 2 floor) and the texts' keys; the client sends the pickup again, confirmed</summary>
[Message(ServerMessages.ObjectRemoveConfirmMessageComposer)]
public sealed partial record ObjectRemoveConfirmMessageComposer(int ItemType, int ItemId, string ConfirmTitle, string ConfirmBody);
