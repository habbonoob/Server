using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>GetModeratorUserInfoMessageEvent</summary>
[Incoming(ClientMessages.GetModeratorUserInfoMessageEvent)]
public sealed partial record GetModeratorUserInfoMessageEvent(int UserId);
