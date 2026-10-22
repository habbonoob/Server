using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.GroupForums;

/// <summary>GetForumStatsMessageEvent</summary>
[Incoming(ClientMessages.GetForumStatsMessageEvent)]
public sealed partial record GetForumStatsMessageEvent(int GroupId);
