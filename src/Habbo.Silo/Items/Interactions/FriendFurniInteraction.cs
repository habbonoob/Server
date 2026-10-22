namespace Habbo.Silo.Items.Interactions;

/// <summary>Friendship furniture — lovelocks, votives, a wanted poster: two friends standing by it lock it
/// together, and it keeps their names, looks and the day for good. The room runs the locking.</summary>
[FurniInteraction("furniture_friend_furni", "furniture_lovelock", "furniture_hween_lovelock", "furniture_wildwest_wanted")]
public sealed class FriendFurniInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;
}
