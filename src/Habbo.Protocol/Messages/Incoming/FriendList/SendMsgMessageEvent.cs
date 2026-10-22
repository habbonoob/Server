using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>3004 SendMsg — a console message to one friend. The confirmation id is the sender's
/// own counter, which comes back on failure so their client knows which line went wrong.</summary>
[Incoming(ClientMessages.SendMsgMessageEvent)]
public sealed partial record SendMsgMessageEvent(int FriendId, string Message, int ConfirmationId);
