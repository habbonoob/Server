namespace Habbo.Domain.Subscriptions;

/// <summary>How much a Builders Club membership lets a player build with. The furni limit is
/// the point of the subscription, so it is stored with the tier rather than worked out from it.
/// </summary>
public sealed class BuildersClubLimits
{
    public int Id { get; set; }

    /// <summary>The product name this applies to, matching <see cref="Subscription.ProductName"/>.
    /// The client treats anything starting with "builders_club" as one.</summary>
    public required string ProductName { get; set; }

    /// <summary>How many items a member may have out at once.</summary>
    public int FurniLimit { get; set; }

    /// <summary>The most any tier of the subscription allows, which the client shows as the
    /// ceiling a member could reach by upgrading.</summary>
    public int MaxFurniLimit { get; set; }

    /// <summary>How long a lapsed membership keeps working for before the items go away.</summary>
    public TimeSpan Grace { get; set; }
}
