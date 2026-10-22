using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>434 MessengerErrorComposer — a message could not be delivered. The client message id
/// is the sender's own counter, so their client can mark the right line as failed.</summary>
[Message(ServerMessages.MessengerErrorComposer)]
public sealed partial record MessengerErrorComposer(int ClientMessageId, int ErrorCode);
