using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.PeerService;

/// <summary>HasClaimedProductMessageEvent — whether the player has claimed a free product</summary>
[Incoming(ClientMessages.HasClaimedProductMessageEvent)]
public sealed partial record HasClaimedProductMessageEvent(string ClaimId);
