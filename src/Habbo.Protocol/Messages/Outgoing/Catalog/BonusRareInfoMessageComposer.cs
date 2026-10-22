using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>BonusRareInfoMessageComposer — the rare given for spending credits, and how many are still to spend</summary>
[Message(ServerMessages.BonusRareInfoMessageComposer)]
public sealed partial record BonusRareInfoMessageComposer(string ProductType, int ProductClassId, int TotalCoinsForBonus, int CoinsStillRequiredToBuy);
