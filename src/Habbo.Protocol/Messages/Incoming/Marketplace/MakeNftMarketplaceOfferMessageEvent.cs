using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>MakeNftMarketplaceOfferMessageEvent (November 2026) — put an NFT (furni whose class name starts "nft_") up
/// for sale for emeralds: its kind as the marketplace numbers it (1 floor, 2 wall), the item and the price. An NFT is
/// offered alone, with no can-make-offer asked first.</summary>
[Incoming(ClientMessages.MakeNftMarketplaceOfferMessageEvent)]
public sealed partial record MakeNftMarketplaceOfferMessageEvent(int NftFurniType, int ItemId, int Price);
