using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendFurni;

/// <summary>FriendFurniCancelLockMessageComposer — the lock was called off</summary>
[Message(ServerMessages.FriendFurniCancelLockMessageComposer)]
public sealed partial record FriendFurniCancelLockMessageComposer(int StuffId);
