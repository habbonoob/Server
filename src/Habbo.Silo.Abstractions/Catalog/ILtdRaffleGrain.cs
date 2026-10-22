using Orleans;

namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>The raffle a limited edition goes on sale with: everyone who tries to buy it in the first seconds of its
/// sale is entered, and when those seconds are up as many as there are left are drawn at random and sold one. Keyed
/// by the offer's id.</summary>
public interface ILtdRaffleGrain : IGrainWithIntegerKey
{
    /// <summary>Enters the player, and tells them so, while the offer's raffle is open; false when it is not, and the
    /// offer sells as it always does.</summary>
    Task<bool> EnterAsync(Guid playerId, ShopOffer offer, string extraData);

    /// <summary>Whether the offer's raffle is open now: while it is, the edition is only to be had through it.</summary>
    Task<bool> IsOpenAsync(ShopOffer offer);
}
