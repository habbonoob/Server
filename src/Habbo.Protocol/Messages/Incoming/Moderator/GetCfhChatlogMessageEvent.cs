using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetCfhChatlogMessageEvent</summary>
[Incoming(ClientMessages.GetCfhChatlogMessageEvent)]
public sealed partial record GetCfhChatlogMessageEvent(int IssueId);
