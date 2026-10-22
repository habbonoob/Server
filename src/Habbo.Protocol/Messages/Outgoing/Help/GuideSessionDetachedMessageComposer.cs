using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionDetachedMessageComposer — the request is no longer the player's to handle</summary>
[Message(ServerMessages.GuideSessionDetachedMessageComposer)]
public sealed partial record GuideSessionDetachedMessageComposer;
