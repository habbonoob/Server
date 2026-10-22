using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.PeerService;

/// <summary>HasClaimedProductResponseMessageComposer — whether the player has claimed a free product</summary>
[Message(ServerMessages.HasClaimedProductResponseMessageComposer)]
public sealed partial record HasClaimedProductResponseMessageComposer(string ClaimId, bool HasClaimed);
