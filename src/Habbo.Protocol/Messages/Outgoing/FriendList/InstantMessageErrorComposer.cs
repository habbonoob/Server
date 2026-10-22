using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.FriendList;

/// <summary>InstantMessageErrorComposer — a console message that did not go through</summary>
[Message(ServerMessages.InstantMessageErrorComposer)]
public sealed partial record InstantMessageErrorComposer(int ErrorCode, int UserId, string Message);
