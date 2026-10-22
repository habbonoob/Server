using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>HabboSearchMessageEvent — find users by name</summary>
[Incoming(ClientMessages.HabboSearchMessageEvent)]
public sealed partial record HabboSearchMessageEvent(string Query);
