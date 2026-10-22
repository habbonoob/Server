using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Session;

/// <summary>ChangeQueueMessageEvent</summary>
[Incoming(ClientMessages.ChangeQueueMessageEvent)]
public sealed partial record ChangeQueueMessageEvent(int Target);
