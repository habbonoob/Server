using Habbo.Domain.Users;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>How many badges of a rarity a profile's owner has.</summary>
[WireStruct]
public sealed partial record BadgeRarityCountData([property: WireFormat(WireType.U8)] BadgeRarity Rarity, int Count);
