using Habbo.Silo.Abstractions.Wired;
namespace Habbo.Silo.Wired.Addons;

/// <summary>Changes how many of the stack's conditions must pass: all, at least one, not all,
/// none, or fewer than, exactly, or more than a number of them. It applies to the conditions it
/// picks, or to all of them when it picks none; any others must still all pass.</summary>
[WiredBox("wf_xtra_or_eval")]
public sealed class ConditionEvaluationAddon : WiredAddon
{
    // eval_mode.0 to .3; a comparison is -1, with its kind and number after
    private const int All = 0;
    private const int AtLeastOne = 1;
    private const int NotAll = 2;
    private const int None = 3;
    private const int Compare = -1;

    public override int Code => AddonCodes.ConditionEvaluation;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.PickedOnly];
    public override WiredPickRule? PickRule => new(item =>
            WiredKinds.FromInteraction(item.Item.Definition!.InteractionName) is WiredKind.Condition or WiredKind.Addon,
        "wiredfurni.error.condition_evaluation_furni");
    public override IReadOnlyList<int> DefaultIntParams => [AtLeastOne, 0, 0];

    /// <summary>Whether the count of passing conditions, out of those it applies to, is enough.</summary>
    public static bool Holds(WiredBox box, int passed, int total) => box.IntParam(0, AtLeastOne) switch
    {
        All => passed == total,
        AtLeastOne => passed > 0,
        NotAll => passed < total,
        None => passed == 0,
        Compare => Conditions.WiredComparison.Holds(box.IntParam(1), passed, box.IntParam(2)),
        _ => passed == total,
    };
}
