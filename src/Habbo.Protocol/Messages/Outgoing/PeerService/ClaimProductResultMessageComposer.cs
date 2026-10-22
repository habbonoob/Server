using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.PeerService;

/// <summary>ClaimProductResultMessageComposer — how claiming a free product went</summary>
[Message(ServerMessages.ClaimProductResultMessageComposer)]
public sealed partial record ClaimProductResultMessageComposer(string ClaimId, int Result)
{
    /// <summary>The results the client has texts for ("claim_product.result.N").</summary>
    public const int Claimed = 0;
    public const int AlreadyClaimed = 1;
    public const int NotFound = 2;
    public const int NotActive = 3;
    public const int Failed = 4;
}
