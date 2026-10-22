using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Addons;

/// <summary>What the by-variable filters share: of what the selectors chose, those holding the
/// chosen variable, sorted by its value or its age, cut down to so many.</summary>
public static class VariableFilter
{
    private const int Amount = 0;
    private const int SortBy = 1;
    private const int AmountOption = 2;
    private const int AmountTarget = 3;

    // variables.sort_by.N
    private const int HighestValue = 0;
    private const int LowestValue = 1;
    private const int OldestCreation = 2;
    private const int LatestCreation = 3;
    private const int OldestUpdate = 4;

    public static IReadOnlyList<int> Defaults => [10, HighestValue, WiredFilter.FixedAmount, WiredVariableTargets.User];

    public static List<T> Filter<T>(WiredBox box, WiredRun run, List<T> chosen, Func<T, WiredHolder> holderOf)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return chosen;
        if (WiredVariableUse.Reference(box, run, box.IntParam(AmountOption) != WiredFilter.FixedAmount, box.IntParam(Amount, 1),
                box.IntParam(AmountTarget), variableIndex: 1, slot: 0) is not { } amount)
            return chosen;

        var holding = chosen
            .Select(x => (Item: x, Value: run.Room.VariableValue(variable, holderOf(x), run),
                Times: run.Room.VariableTimes(variable, holderOf(x), run)))
            .Where(x => run.Room.HasVariable(variable, holderOf(x.Item), run))
            .ToList();

        IEnumerable<(T Item, int? Value, WiredVariableValue? Times)> sorted = box.IntParam(SortBy) switch
        {
            LowestValue => holding.OrderBy(x => x.Value ?? 0),
            OldestCreation => holding.OrderBy(x => x.Times?.Created ?? DateTimeOffset.MaxValue),
            LatestCreation => holding.OrderByDescending(x => x.Times?.Created ?? DateTimeOffset.MinValue),
            OldestUpdate => holding.OrderBy(x => x.Times?.Updated ?? DateTimeOffset.MaxValue),
            HighestValue => holding.OrderByDescending(x => x.Value ?? 0),
            _ => holding.OrderByDescending(x => x.Times?.Updated ?? DateTimeOffset.MinValue),
        };

        return sorted.Take(Math.Clamp(amount, 0, WiredFilter.MaxAmount)).Select(x => x.Item).ToList();
    }
}

/// <summary>Cuts the furni the stack's selectors chose down to those with the chosen variable, sorted, so many.</summary>
[WiredBox("wf_xtra_filter_furni_by_var")]
public sealed class FurniVariableFilterAddon : WiredAddon
{
    public override int Code => AddonCodes.FurniVariableFilter;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => VariableFilter.Defaults;
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
}

/// <summary>Cuts the users the stack's selectors chose down to those with the chosen variable, sorted, so many.</summary>
[WiredBox("wf_xtra_filter_users_by_var")]
public sealed class UserVariableFilterAddon : WiredAddon
{
    public override int Code => AddonCodes.UserVariableFilter;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.SelectorFurni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.SelectorUsers];
    public override IReadOnlyList<int> DefaultIntParams => VariableFilter.Defaults;
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;
}
