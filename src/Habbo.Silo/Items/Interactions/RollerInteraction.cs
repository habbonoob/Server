using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A roller: every few ticks it moves what stands on it — the furniture stacked on it and
/// an avatar standing there — one tile the way it faces. The room runs the rollers together.
///
/// It carries no state of its own. Habbo's catalog names "1" for it, the state a placed roller shows, but an owned
/// roller holds nothing (so the inventory stacks them, as Habbo's does), and in a room it is shown running.</summary>
[FurniInteraction(Name)]
public sealed class RollerInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives rollers.</summary>
    public const string Name = "roller";

    /// <summary>The state a roller in a room shows.</summary>
    private const string Running = "1";

    /// <summary>Nothing of the catalog's: a bought roller holds no state.</summary>
    public override void Grant(FurniGrant grant)
    {
    }

    public override FurniDataView InRoom(FurniDataView data) => FurniDataView.WithState(Running);
}
