namespace Habbo.Domain.Catalog;

/// <summary>Where a player stands with a targeted offer: how the client last showed it, and how many
/// they bought.</summary>
public sealed class TargetedOfferState
{
    public Guid UserId { get; set; }

    public int TargetedOfferId { get; set; }

    public int TrackingState { get; set; }

    public int Purchased { get; set; }
}
