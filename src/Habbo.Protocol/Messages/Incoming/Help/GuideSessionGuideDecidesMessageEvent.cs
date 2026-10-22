using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionGuideDecidesMessageEvent — take the request offered, or pass it on</summary>
[Incoming(ClientMessages.GuideSessionGuideDecidesMessageEvent)]
public sealed partial record GuideSessionGuideDecidesMessageEvent(bool Accept);
