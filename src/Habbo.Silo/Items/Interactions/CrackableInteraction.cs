using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A crackable (a bonus bag): hit by those with rights until it cracks open, when the prize it holds stands
/// where it stood. It starts whole, with the hits it takes (Habbo's bonus bag: 0 of 1). The room does the cracking.
/// </summary>
[FurniInteraction(Name)]
public sealed class CrackableInteraction : FurniInteraction
{
    /// <summary>The interaction name seed/furni_interactions.json gives crackables.</summary>
    public const string Name = "crackable";

    /// <summary>How many hits a bonus bag takes (habbo.com, October 2026: "Hits: 0 / 1").</summary>
    public const int BonusBagHits = 1;

    /// <summary>What a cracked one shows before it goes (habbo.com: state 2, then removed).</summary>
    public const string CrackedState = "2";

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override void Grant(FurniGrant grant) =>
        FurniDataStore.Attach(grant.Db, grant.Item, Whole(BonusBagHits));

    /// <summary>One given before bags could crack holds no count, and the client's info stand needs one: it shows whole.</summary>
    public override FurniDataView InRoom(FurniDataView data) =>
        data.Type == (int)FurniDataType.Crackable ? data : Whole(BonusBagHits);

    public static FurniDataView Whole(int target) =>
        FurniDataView.Plain with { Type = (int)FurniDataType.Crackable, State = string.Empty, Hits = 0, Target = target };
}
