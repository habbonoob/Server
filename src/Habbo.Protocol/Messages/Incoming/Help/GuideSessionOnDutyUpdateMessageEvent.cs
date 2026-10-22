using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionOnDutyUpdateMessageEvent — go on or off duty, and which requests to take</summary>
[Incoming(ClientMessages.GuideSessionOnDutyUpdateMessageEvent)]
public sealed partial record GuideSessionOnDutyUpdateMessageEvent(bool OnDuty, bool HandleTours, bool HandleHelps, bool HandleChatReviews);
