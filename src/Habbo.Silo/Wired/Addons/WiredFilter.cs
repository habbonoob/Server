namespace Habbo.Silo.Wired.Addons;

/// <summary>The filter add-ons' shared reading of their amount.</summary>
public static class WiredFilter
{
    /// <summary>The amount is the number the dialog holds, not a variable's value.</summary>
    public const int FixedAmount = 0;

    public const int MaxAmount = 1000;

    private const int Amount = 0;
    private const int AmountOption = 1;
    private const int AmountTarget = 2;

    /// <summary>So many of what was chosen, at random: the dialog's number, or a variable's value.</summary>
    public static List<T> Filter<T>(WiredBox box, WiredRun run, List<T> chosen)
    {
        var amount = Variables.WiredVariableUse.Reference(box, run, box.IntParam(AmountOption) != FixedAmount,
            box.IntParam(Amount, 1), box.IntParam(AmountTarget), variableIndex: 0, slot: 0);
        return amount is { } take
            ? chosen.OrderBy(_ => run.Room.Random.Next()).Take(Math.Clamp(take, 0, MaxAmount)).ToList()
            : chosen;
    }
}
