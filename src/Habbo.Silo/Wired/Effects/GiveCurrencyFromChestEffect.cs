using Habbo.Protocol.Messages.Outgoing.Vault;
using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Effects;

/// <summary>Gives credits from its credit chests into the users' earnings, under the category it names.</summary>
[WiredBox("wf_act_give_currency")]
public sealed class GiveCurrencyFromChestEffect : GiveFromChestEffect
{
    /// <summary>The earnings categories the dialog offers.</summary>
    private static readonly byte[] Categories = [IncomeRewardData.Games, IncomeRewardData.Agencies];

    public override int Code => EffectCodes.GiveCurrencyFromChest;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<int> DefaultIntParams => [0, 1, 0, WiredVariableTargets.Context, 1, IncomeRewardData.Games];
    public override IReadOnlyList<int> ResetIntParams => [IncomeRewardData.Games];

    protected override Task GiveAsync(WiredBox box, WiredRun run, List<RoomItemStore.PlacedItem> chests, RoomEntityStore.RoomEntity user,
        int? amount, WiredChestGift gift)
    {
        var category = Categories.Contains((byte)box.IntParam(Extra)) ? (byte)box.IntParam(Extra) : Categories[0];
        return run.Room.Chests.GiveCoinsAsync(chests, user, amount, category, gift);
    }
}
