using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendFurni;

/// <summary>FriendFurniConfirmLockMessageEvent — confirm or turn down locking a friendship furni</summary>
[Incoming(ClientMessages.FriendFurniConfirmLockMessageEvent)]
public sealed partial record FriendFurniConfirmLockMessageEvent(int StuffId, bool Confirmed);
