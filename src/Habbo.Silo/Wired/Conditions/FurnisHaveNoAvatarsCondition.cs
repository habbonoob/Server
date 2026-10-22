namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when no user stands on the picked furni: on none of them, or not on some one,
/// as the dialog chooses.</summary>
[WiredBox("wf_cnd_not_hv_avtrs")]
public sealed class FurnisHaveNoAvatarsCondition : WiredCondition
{
    public override int Code => ConditionCodes.NotFurnisHaveAvatars;

    /// <summary>Habbo sends it inverted, though its own dialog says the negative and it does not invert its test.</summary>
    public override bool InvertedInDialog => true;
    public override bool PicksFurni => true;
    // its radio: 1 any, 0 all (the client's requireall texts); a new box on Habbo: all
    private const int RequireAll = 0;
    private const int RequireAny = 1;

    public override IReadOnlyList<int> DefaultIntParams => [RequireAll];

    public override bool Passes(WiredBox box, WiredRun run) =>
        WiredQuantity.Holds(box.IntParam(0) == RequireAll, run.Furni(box),
            item => !FurnisHaveAvatarsCondition.HasAvatar(item, run.Room));
}
