using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>A Habbicon in its collection: its asset name (habbicon_&lt;name&gt;_name is its title), where the player
/// stands with it, and its price, credits and activity points together (none when it is not sold).</summary>
[WireStruct]
public sealed partial record HabbiconData(
    int HabbiconId,
    string Name,
    int CollectionId,
    HabbiconState State,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType);
