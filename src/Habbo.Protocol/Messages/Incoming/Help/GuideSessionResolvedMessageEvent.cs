using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionResolvedMessageEvent — the guide is done</summary>
[Incoming(ClientMessages.GuideSessionResolvedMessageEvent)]
public sealed partial record GuideSessionResolvedMessageEvent;
