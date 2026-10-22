namespace Habbo.Silo.Items.Interactions;

/// <summary>An invisible click tile: furniture nobody sees, for the "click tile" trigger to listen
/// on. seed/furni_interactions.json names them.</summary>
[FurniInteraction(Name)]
public sealed class ClickTileInteraction : FurniInteraction
{
    public const string Name = "click_tile";
}
