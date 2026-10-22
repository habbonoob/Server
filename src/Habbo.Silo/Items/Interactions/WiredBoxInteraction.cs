namespace Habbo.Silo.Items.Interactions;

/// <summary>Wired boxes. Using one opens its dialog, which the room does itself (the room's wired
/// holds the settings), so the behaviour only says who may: those who control the room.</summary>
[FurniInteraction("wired_trigger", "wired_effect", "wired_condition", "wired_selector", "wired_addon", "wired_variable")]
public sealed class WiredBoxInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;
}
