namespace Habbo.Silo.Items.Interactions;

/// <summary>A hopper: jump in and come out of another hopper of its kind, in a random room of the hotel. Some hoppers
/// are for club members only, or for those in costume (Rooms:Hoppers). The room does the jumping.</summary>
[FurniInteraction("furniture_random_teleport")]
public sealed class HopperInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;
}
