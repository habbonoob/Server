using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>1276 MessengerInit — the client asks for the messenger's limits, groups and friends.</summary>
[Incoming(ClientMessages.MessengerInitMessageEvent)]
public sealed partial record MessengerInitMessageEvent;
