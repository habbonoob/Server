namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>Where a player stands with a Habbicon, as the Habbicons Collection reads it.</summary>
public enum HabbiconState
{
    /// <summary>Not owned, and sold: a price to buy it at.</summary>
    Purchasable = 0,

    /// <summary>A collection's reward, the collection complete: ready to claim.</summary>
    Claimable = 1,

    Owned = 2,

    /// <summary>Owned and starred, listed under the favourites tab.</summary>
    Favourite = 3,

    /// <summary>A collection's reward, the collection not yet complete.</summary>
    Locked = 4,

    /// <summary>Not owned, and not sold: only given, as a reward track's prize is.</summary>
    Reward = 5,
}
