using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Catalog;

/// <summary>DirectSMSClubBuyAvailableMessageComposer — whether the club can be bought by text message: an empty link says no</summary>
[Message(ServerMessages.DirectSMSClubBuyAvailableMessageComposer)]
public sealed partial record DirectSMSClubBuyAvailableMessageComposer(string PricePointUrl, string Market, int LengthInDays);
