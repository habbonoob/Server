using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendFurni;

/// <summary>FriendFurniStartConfirmationMessageComposer — asks one of two friends to lock a friendship furni</summary>
[Message(ServerMessages.FriendFurniStartConfirmationMessageComposer)]
public sealed partial record FriendFurniStartConfirmationMessageComposer(int StuffId, bool IsOwner);
