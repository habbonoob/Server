using Habbo.Silo.Rooms;
using Habbo.Silo.Wired.Conditions;
using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Selectors;

/// <summary>What the with-variable selectors share: everyone holding the chosen variable, and, when
/// the dialog selects by value, only those whose value compares as chosen with a number or another
/// variable's value (read from the selector's merged slot).</summary>
public static class WithVariable
{
    private const int Comparison = 0;
    private const int ByValue = 1;
    private const int Number = 2;
    private const int ReferenceTarget = 4;

    /// <summary>The "select by value" option, one more than the reference option it carries.</summary>
    private const int NotByValue = 0;
    private const int FromVariable = 2;

    public static IReadOnlyList<int> Defaults => [WiredComparison.HigherThan, NotByValue, 0, 0, WiredVariableTargets.Global];

    public static IEnumerable<WiredHolder> Select(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return [];

        var holders = run.Room.VariableHolders(variable);
        if (box.IntParam(ByValue) == NotByValue)
            return holders.Select(h => h.Holder);

        if (WiredVariableUse.Reference(box, run, box.IntParam(ByValue) == FromVariable, WiredVariableUse.Number(box, Number),
                box.IntParam(ReferenceTarget), variableIndex: 1, slot: 0) is not { } reference)
            return [];

        return holders.Where(h => WiredComparison.Holds(box.IntParam(Comparison), h.Value, reference)).Select(h => h.Holder);
    }
}

/// <summary>Chooses the furni holding the chosen variable (with a value, if the dialog says, that compares as chosen).</summary>
[WiredBox("wf_slc_furni_with_var")]
public sealed class FurniWithVariableSelector : WiredFurniSelector
{
    public override int Code => SelectorCodes.FurniWithVariable;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => WithVariable.Defaults;
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override IEnumerable<RoomItemStore.PlacedItem> Select(WiredBox box, WiredRun run) =>
        WithVariable.Select(box, run).Select(h => h.Item).OfType<RoomItemStore.PlacedItem>();
}

/// <summary>Chooses the users holding the chosen variable (with a value, if the dialog says, that compares as chosen).</summary>
[WiredBox("wf_slc_users_with_var")]
public sealed class UsersWithVariableSelector : WiredUserSelector
{
    public override int Code => SelectorCodes.UsersWithVariable;
    public override bool AllowsWallFurni => true;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => WithVariable.Defaults;
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override IEnumerable<RoomEntityStore.RoomEntity> Select(WiredBox box, WiredRun run) =>
        WithVariable.Select(box, run).Select(h => h.User).OfType<RoomEntityStore.RoomEntity>();
}
