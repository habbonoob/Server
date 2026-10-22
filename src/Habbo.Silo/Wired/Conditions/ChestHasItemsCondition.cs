using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when what its chests hold together — credits, or items — compares as chosen with a number or a
/// variable's value. A locked chest, or one that is not a Wired chest, counts as holding nothing.</summary>
[WiredBox("wf_cnd_chest_has_items")]
public class ChestHasItemsCondition : WiredCondition
{
    private const int Number = 0;
    private const int ReferenceOption = 1;
    private const int ReferenceTarget = 2;
    private const int Comparison = 3;
    private const int FromVariable = 1;

    public override int Code => ConditionCodes.ChestHasItems;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni, WiredSlot.Furni.DefaultingTo(WiredSources.SelectedFurni)];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users.DefaultingTo(WiredSources.SelectedUsers)];
    public override IReadOnlyList<int> DefaultIntParams => [0, 0, WiredVariableTargets.Context, WiredComparison.HigherThan];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    /// <summary>Which furni slot holds the chests, and which the variable's furni.</summary>
    protected virtual int ChestSlot => 0;
    protected virtual int ReferenceFurniSlot => 1;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        // the variable's holders: its furni from the reference slot, its users from the only user slot
        var target = box.IntParam(ReferenceTarget);
        var slot = target == WiredVariableTargets.User ? 0 : ReferenceFurniSlot;
        if (WiredVariableUse.Reference(box, run, box.IntParam(ReferenceOption) == FromVariable, box.IntParam(Number), target,
                variableIndex: 0, slot: slot) is not { } reference)
            return false;

        var kinds = Kinds(box, run);
        var held = run.Furni(box, ChestSlot).Sum(chest => run.Room.Chests.Count(chest, kinds));
        return WiredComparison.Holds(box.IntParam(Comparison), held, reference) != IsNegative;
    }

    /// <summary>The kinds counted; every kind unless the condition names some.</summary>
    protected virtual IReadOnlySet<Habbo.Silo.Chests.ChestKind>? Kinds(WiredBox box, WiredRun run) => null;
}
