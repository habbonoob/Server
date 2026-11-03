using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Collectibles;

/// <summary>EmeraldBalanceMessageComposer — the player's emeralds, which the client keeps apart from its other
/// currencies (the NFT marketplace prices in them).</summary>
[Message(ServerMessages.EmeraldBalanceMessageComposer)]
public sealed partial record EmeraldBalanceMessageComposer(int Emeralds);
