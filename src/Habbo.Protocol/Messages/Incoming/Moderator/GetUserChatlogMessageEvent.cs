using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetUserChatlogMessageEvent</summary>
[Incoming(ClientMessages.GetUserChatlogMessageEvent)]
public sealed partial record GetUserChatlogMessageEvent(int UserId);
