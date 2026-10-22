using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>1739 LtdRaffleEnteredMessageComposer — the purchase of a limited edition went into its raffle; the
/// confirmation dialog waits for the draw.</summary>
[Message(ServerMessages.LtdRaffleEnteredMessageComposer)]
public sealed partial record LtdRaffleEnteredMessageComposer(string ClassName);
