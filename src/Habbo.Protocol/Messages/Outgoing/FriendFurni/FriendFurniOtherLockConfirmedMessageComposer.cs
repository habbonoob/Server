using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendFurni;

/// <summary>FriendFurniOtherLockConfirmedMessageComposer — the friend has confirmed the lock</summary>
[Message(ServerMessages.FriendFurniOtherLockConfirmedMessageComposer)]
public sealed partial record FriendFurniOtherLockConfirmedMessageComposer(int StuffId);
