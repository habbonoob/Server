using Habbo.Silo.Chests;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A chest: using it opens it, for whoever may look inside. It starts with its settings as map data;
/// the room runs everything else about it.</summary>
public abstract class ChestInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    /// <summary>Whether it holds credits rather than furniture.</summary>
    public abstract bool HoldsCoins { get; }

    public override void Grant(FurniGrant grant) =>
        FurniDataStore.Attach(grant.Db, grant.Item, ChestSettings.Initial.ToData());
}
