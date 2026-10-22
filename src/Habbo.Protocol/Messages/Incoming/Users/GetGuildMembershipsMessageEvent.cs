using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetGuildMembershipsMessageEvent — the player's groups</summary>
[Incoming(ClientMessages.GetGuildMembershipsMessageEvent)]
public sealed partial record GetGuildMembershipsMessageEvent;
