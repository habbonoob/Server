using Habbo.Protocol.Messages.Outgoing.Habbicons;

namespace Habbo.Silo.Habbicons;

/// <summary>Where a player stands with the Habbicons Collection, from the Habbicons they own (by id, and whether each is
/// starred): a Habbicon is theirs, for sale, or only given; a collection is complete once they own each of its
/// Habbicons, and its reward is claimable then, locked until then.</summary>
public static class HabbiconRules
{
    /// <summary>How many Habbicons the chat input's picker lists as used last.</summary>
    public const int RecentLimit = 10;

    /// <summary>A collection without a reward, as the client reads it.</summary>
    public const int NoReward = 0;

    public static (HabbiconCollection Collection, HabbiconEntry Habbicon)? Find(IEnumerable<HabbiconCollection> collections, int habbiconId)
    {
        foreach (var collection in collections)
        {
            if (collection.Habbicons.FirstOrDefault(h => h.Id == habbiconId) is { } habbicon)
                return (collection, habbicon);
            if (collection.Reward is { } reward && reward.Id == habbiconId)
                return (collection, reward);
        }

        return null;
    }

    public static bool IsReward(HabbiconCollection collection, int habbiconId) => collection.Reward?.Id == habbiconId;

    public static HabbiconState OwnedState(bool favourite) => favourite ? HabbiconState.Favourite : HabbiconState.Owned;

    /// <summary>A collection's own Habbicon: the player's, for sale, or only given.</summary>
    public static HabbiconState StateOf(HabbiconEntry habbicon, IReadOnlyDictionary<int, bool> owned) =>
        owned.TryGetValue(habbicon.Id, out var favourite) ? OwnedState(favourite)
        : habbicon.IsSold ? HabbiconState.Purchasable
        : HabbiconState.Reward;

    public static bool IsComplete(HabbiconCollection collection, IReadOnlyDictionary<int, bool> owned) =>
        collection.Habbicons.Count > 0 && collection.Habbicons.All(h => owned.ContainsKey(h.Id));

    /// <summary>A collection's reward: the player's once claimed, claimable once the collection is complete.</summary>
    public static HabbiconState RewardStateOf(HabbiconCollection collection, IReadOnlyDictionary<int, bool> owned) =>
        collection.Reward is { } reward && owned.TryGetValue(reward.Id, out var favourite) ? OwnedState(favourite)
        : IsComplete(collection, owned) ? HabbiconState.Claimable
        : HabbiconState.Locked;

    /// <summary>Either kind of Habbicon, as the player stands with it.</summary>
    public static HabbiconState StateOf(HabbiconCollection collection, HabbiconEntry habbicon, IReadOnlyDictionary<int, bool> owned) =>
        IsReward(collection, habbicon.Id) ? RewardStateOf(collection, owned) : StateOf(habbicon, owned);

    /// <summary>A Habbicon the player may buy: a collection's own, sold, and not theirs yet.</summary>
    public static bool CanBuy(HabbiconCollection collection, HabbiconEntry habbicon, IReadOnlyDictionary<int, bool> owned) =>
        !IsReward(collection, habbicon.Id) && StateOf(habbicon, owned) == HabbiconState.Purchasable;

    /// <summary>What buying the whole set hands over: those of its Habbicons the player has not got. A set sold as one is
    /// sold until it is complete, whatever of it the player already owns.</summary>
    public static IReadOnlyList<HabbiconEntry> SetPurchase(HabbiconCollection collection, IReadOnlyDictionary<int, bool> owned) =>
        collection.IsSold && !IsComplete(collection, owned)
            ? collection.Habbicons.Where(h => !owned.ContainsKey(h.Id)).ToList()
            : [];

    public static bool CanClaim(HabbiconCollection collection, int habbiconId, IReadOnlyDictionary<int, bool> owned) =>
        IsReward(collection, habbiconId) && RewardStateOf(collection, owned) == HabbiconState.Claimable;

    public static HabbiconData DataOf(HabbiconCollection collection, HabbiconEntry habbicon, IReadOnlyDictionary<int, bool> owned) =>
        new(habbicon.Id, habbicon.Name, collection.Id, StateOf(collection, habbicon, owned),
            habbicon.PriceCredits, habbicon.PriceActivityPoints, habbicon.ActivityPointType);

    public static HabbiconCollectionData DataOf(HabbiconCollection collection, IReadOnlyDictionary<int, bool> owned) =>
        new(collection.Id, collection.Name, IsComplete(collection, owned), collection.Reward?.Id ?? NoReward,
            RewardStateOf(collection, owned), collection.PriceCredits, collection.PriceActivityPoints, collection.ActivityPointType,
            collection.Habbicons.Select(h => DataOf(collection, h, owned)).ToList());

    /// <summary>The Habbicons the player owns, in the hub's order (a collection's, then its reward), each starred or not.</summary>
    public static IReadOnlyList<UserHabbiconData> OwnedData(IEnumerable<HabbiconCollection> collections, IReadOnlyDictionary<int, bool> owned) =>
        collections
            .SelectMany(c => c.Reward is { } reward ? c.Habbicons.Append(reward) : c.Habbicons)
            .Where(h => owned.ContainsKey(h.Id))
            .Select(h => new UserHabbiconData(h.Id, OwnedState(owned[h.Id])))
            .ToList();

    /// <summary>How many of the collections the player has completed.</summary>
    public static int CompletedCollections(IEnumerable<HabbiconCollection> collections, IReadOnlyDictionary<int, bool> owned) =>
        collections.Count(c => IsComplete(c, owned));
}
