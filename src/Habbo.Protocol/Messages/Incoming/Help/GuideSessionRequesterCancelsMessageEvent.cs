using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionRequesterCancelsMessageEvent — the requester no longer wants help</summary>
[Incoming(ClientMessages.GuideSessionRequesterCancelsMessageEvent)]
public sealed partial record GuideSessionRequesterCancelsMessageEvent;
