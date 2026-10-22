using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Marketplace;

/// <summary>MakeOfferMessageEvent — put inventory items up for sale at a price: one, or since September 2026 several
/// of the same kind at once.</summary>
[Incoming(ClientMessages.MakeOfferMessageEvent)]
public sealed partial record MakeOfferMessageEvent(
    int Price,
    int FurniType,
    [property: WireUntil(ClientReleases.September2026)] int? ItemId,
    [property: WireSince(ClientReleases.September2026)] IReadOnlyList<int>? ItemIds);
