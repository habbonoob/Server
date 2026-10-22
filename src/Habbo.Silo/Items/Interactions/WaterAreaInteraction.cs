namespace Habbo.Silo.Items.Interactions;

/// <summary>A pond, a shore, a patch of water. The client runs it as plain multistate furniture,
/// but its state is not a look the item steps through: it is the border mask the room works out
/// from where the water meets land. So nobody uses one, and it is granted with no state of its
/// own — the room fills that in as soon as the item is placed.</summary>
[FurniInteraction(WaterArea)]
public sealed class WaterAreaInteraction : FurniInteraction
{
    /// <summary>The name <c>seed/furni_interactions.json</c> maps the water visualization to.</summary>
    public const string WaterArea = "water_area";
}
