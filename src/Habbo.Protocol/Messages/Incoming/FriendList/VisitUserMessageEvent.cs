using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>VisitUserMessageEvent — go to the room a user is in (:visit)</summary>
[Incoming(ClientMessages.VisitUserMessageEvent)]
public sealed partial record VisitUserMessageEvent(string UserName);
