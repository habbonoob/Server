using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionFeedbackMessageEvent — whether the guide was any good</summary>
[Incoming(ClientMessages.GuideSessionFeedbackMessageEvent)]
public sealed partial record GuideSessionFeedbackMessageEvent(bool Recommend);
