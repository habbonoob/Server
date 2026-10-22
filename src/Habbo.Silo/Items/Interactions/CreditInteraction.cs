using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Credit furniture: a gold bar, a coin, a suitcase. Exchanging it pays out what the
/// item is worth and destroys it — the worth comes from the item's own SWF, so a hotel cannot
/// accidentally pay out more than Habbo's own furniture says it is.
///
/// Only the item's owner may exchange it, however it got into the room: everyone can see it, and
/// the client asks whoever double-clicks it.</summary>
[FurniInteraction("furniture_credit", "furniture_nft_credit", "FURNITURE_NFT_CREDIT")]
public sealed class CreditInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    /// <summary>Credit furniture arrives with no data at all. The catalog product repeats the
    /// item's worth in its parameter, which is not a state the item can be in — storing it would
    /// only stop identical coins from stacking into one inventory tile.</summary>
    public override void Grant(FurniGrant grant)
    {
    }

    public override async Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Redeem || use.Item.Item.OwnerId != use.User.Player.Id)
            return;

        // Nothing to pay out: the SWF never said what this one is worth, so it is not exchanged.
        if (use.Item.Item.Definition!.CreditValue is not { } credits || credits <= 0)
            return;

        if (use.Item.Temporary || !await use.Room.RemoveAsync(use.Item))
            return;

        await use.Room.GiveCreditsAsync(use.User, credits);
    }
}
