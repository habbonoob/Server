using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Habbicons;

/// <summary>Habbicons: the small pictures a player shows over their avatar in a room or sends in the console. They come
/// in collections (sets), some sold (credits and activity points together), the rest only given, and owning every one
/// of a set earns its reward Habbicon, claimed from the Habbicons Collection. Every call answers the player who made
/// it.</summary>
public interface IHabbiconService
{
    /// <summary>What Habbo sends at login: the Habbicons owned (UserHabbicons), then the collections (HabbiconShopData).</summary>
    Task SendLoginAsync(PlayerData player);

    Task SendShopDataAsync(PlayerData player);

    Task SendInfoAsync(PlayerData player, int habbiconId);

    /// <summary>Buys a Habbicon that is for sale; the client waits for PurchaseOK or a refusal.</summary>
    Task BuyAsync(PlayerData player, int habbiconId);

    /// <summary>Buys what the player lacks of a set sold as one, for the set's price.</summary>
    Task BuyCollectionAsync(PlayerData player, int collectionId);

    /// <summary>Claims a complete set's reward Habbicon.</summary>
    Task ClaimAsync(PlayerData player, int habbiconId);

    Task SetFavouriteAsync(PlayerData player, int habbiconId, bool favourite);

    /// <summary>Notes a Habbicon as used (it heads the recently used), if the player owns it and is not using them too
    /// fast; false otherwise. Showing it is the room's.</summary>
    Task<bool> UseAsync(Guid playerId, int habbiconId);

    /// <summary>The asset name of a Habbicon the player owns, or null: what an older client is shown in its place.</summary>
    Task<string?> OwnedNameAsync(Guid playerId, int habbiconId);

    /// <summary>Gives a Habbicon (a reward track's prize, a staff gift); false when there is no such Habbicon or the
    /// player already owns it. The client tells the player of it.</summary>
    Task<bool> GrantAsync(Guid playerId, int habbiconId);
}
