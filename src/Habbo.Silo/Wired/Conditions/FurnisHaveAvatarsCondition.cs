namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when users stand on the picked furni: on every one, or on any one, as the dialog
/// chooses.</summary>
[WiredBox("wf_cnd_furnis_hv_avtrs")]
public sealed class FurnisHaveAvatarsCondition : WiredCondition
{
    public override int Code => ConditionCodes.FurnisHaveAvatars;
    public override bool PicksFurni => true;
    // its radio: 0 any, 1 all (the client's requireall texts); a new box on Habbo: all
    private const int RequireAll = 1;
    private const int RequireAny = 0;

    public override IReadOnlyList<int> DefaultIntParams => [RequireAll];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run) =>
        WiredQuantity.Holds(box.IntParam(0) == RequireAll, run.Furni(box), item => HasAvatar(item, run.Room));

    public static bool HasAvatar(Rooms.RoomItemStore.PlacedItem item, IWiredRoom room) =>
        room.Entities.Entities.Any(user => WiredFurni.IsUnder(user, item));
}
