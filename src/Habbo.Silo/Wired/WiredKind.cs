using Habbo.Silo.Abstractions.Wired;

namespace Habbo.Silo.Wired;

public static class WiredKinds
{
    /// <summary>The family an item's interaction name puts it in, or null for anything that is not a
    /// wired box (seed/furni_interactions.json names every wf_ item by its family).</summary>
    public static WiredKind? FromInteraction(string interactionName) => interactionName switch
    {
        "wired_trigger" => WiredKind.Trigger,
        "wired_effect" => WiredKind.Effect,
        "wired_condition" => WiredKind.Condition,
        "wired_selector" => WiredKind.Selector,
        "wired_addon" => WiredKind.Addon,
        "wired_variable" => WiredKind.Variable,
        _ => null,
    };
}
