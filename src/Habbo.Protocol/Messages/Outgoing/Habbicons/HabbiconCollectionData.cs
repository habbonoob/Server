using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>A collection (a set) of Habbicons: its name (habbicon_collection_&lt;name&gt;_name is its title), whether
/// the player owns all of it, its reward Habbicon (0 for none) and where they stand with it, the price of the whole
/// set (none when it is not sold as one), and its Habbicons.</summary>
[WireStruct]
public sealed partial record HabbiconCollectionData(
    int CollectionId,
    string Name,
    bool Completed,
    int RewardHabbiconId,
    HabbiconState RewardState,
    int PriceCredits,
    int PriceActivityPoints,
    int ActivityPointType,
    IReadOnlyList<HabbiconData> Habbicons);
