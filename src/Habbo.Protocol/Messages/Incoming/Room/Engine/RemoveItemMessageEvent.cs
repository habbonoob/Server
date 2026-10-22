using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Engine;

/// <summary>RemoveItemMessageEvent — a note torn off the wall</summary>
[Incoming(ClientMessages.RemoveItemMessageEvent)]
public sealed partial record RemoveItemMessageEvent(int ItemId);
