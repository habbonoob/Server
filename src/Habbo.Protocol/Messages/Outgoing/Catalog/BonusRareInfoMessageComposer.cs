using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>BonusRareInfoMessageComposer — the rare given for spending credits, and how many are still to spend. The
/// client's parser calls the first field the product type, but looks it up in productdata: habbo.com sends the product
/// code ("bonusbag26_3", 18432, 120, 10 on 25 October 2026).</summary>
[Message(ServerMessages.BonusRareInfoMessageComposer)]
public sealed partial record BonusRareInfoMessageComposer(string ProductCode, int ProductClassId, int TotalCoinsForBonus, int CoinsStillRequiredToBuy);
