using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionInviteRequesterMessageEvent — invite the requester to the guide's room</summary>
[Incoming(ClientMessages.GuideSessionInviteRequesterMessageEvent)]
public sealed partial record GuideSessionInviteRequesterMessageEvent;
