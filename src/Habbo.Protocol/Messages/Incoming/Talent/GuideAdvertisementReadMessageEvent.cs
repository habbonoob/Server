using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Talent;

/// <summary>GuideAdvertisementReadMessageEvent — the player read what the guides do, from the help or the talent track</summary>
[Incoming(ClientMessages.GuideAdvertisementReadMessageEvent)]
public sealed partial record GuideAdvertisementReadMessageEvent;
