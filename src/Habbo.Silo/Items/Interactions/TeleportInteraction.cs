
using Habbo.Domain.Furniture.Data;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A teleport. What is bought is the link, not the door: one purchase hands over two
/// items pointing at each other, and stepping into either comes out of the other. Anyone may use
/// one; the room does the travelling.</summary>
[FurniInteraction("teleport")]
public sealed class TeleportInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    public override void Grant(FurniGrant grant)
    {
        base.Grant(grant);

        var partner = grant.AddItem();
        grant.Db.FurniTeleportLinks.Add(new FurniTeleportLink { Item = grant.Item, LinkedItem = partner });
        grant.Db.FurniTeleportLinks.Add(new FurniTeleportLink { Item = partner, LinkedItem = grant.Item });
    }
}
