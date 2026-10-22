namespace Habbo.Domain.Subscriptions;

/// <summary>The product names the client recognises. It branches on these by name, so they are
/// part of the protocol rather than names the hotel is free to pick.</summary>
public static class SubscriptionProducts
{
    /// <summary>What the client drives the purse and the catalog from. It accepts
    /// "club_habbo" for the same thing, but only ever sends this one.</summary>
    public const string HabboClub = "habbo_club";

    /// <summary>The client treats any product name starting with this as a Builders Club,
    /// which is how the tiers are told apart.</summary>
    public const string BuildersClubPrefix = "builders_club";
}
