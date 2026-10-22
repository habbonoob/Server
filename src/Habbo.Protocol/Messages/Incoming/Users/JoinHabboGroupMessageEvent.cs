using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>JoinHabboGroupMessageEvent — join, or ask to</summary>
[Incoming(ClientMessages.JoinHabboGroupMessageEvent)]
public sealed partial record JoinHabboGroupMessageEvent(int GroupId);
