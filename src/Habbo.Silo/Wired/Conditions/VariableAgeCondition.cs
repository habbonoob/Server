using Habbo.Silo.Wired.Variables;

namespace Habbo.Silo.Wired.Conditions;

/// <summary>Passes when the chosen variable was given, or last changed, less or more than so long ago.</summary>
[WiredBox("wf_cnd_var_age_match")]
public sealed class VariableAgeCondition : WiredCondition
{
    private const int Target = 0;
    private const int Comparison = 1;
    private const int CompareValue = 2;
    private const int Amount = 3;
    private const int Unit = 5;

    // variables.compare_value.N
    private const int CreationTime = 0;

    // variables.duration.N
    private const int Milliseconds = 0;
    private const int Seconds = 1;
    private const int Minutes = 2;
    private const int Hours = 3;
    private const int Days = 4;
    private const int Weeks = 5;
    private const int Months = 6;
    private const int DaysPerWeek = 7;

    public override int Code => ConditionCodes.VariableAge;
    public override bool AllowsWallFurni => true;
    public override byte QuantifierType => WiredQuantifierTypes.Variables;
    public override IReadOnlyList<WiredSlot> FurniSlots => [WiredSlot.Furni];
    public override IReadOnlyList<WiredSlot> UserSlots => [WiredSlot.Users];
    public override IReadOnlyList<int> DefaultIntParams =>
        [WiredVariableTargets.User, WiredComparison.LowerThan, CreationTime, 0, 0, Seconds];
    public override IReadOnlyList<int> ResetIntParams => DefaultIntParams;

    public override bool Passes(WiredBox box, WiredRun run)
    {
        if (WiredVariableUse.Variable(box, run.Room) is not { } variable)
            return false;

        var now = run.Room.Now();
        var amount = WiredVariableUse.Number(box, Amount);
        var threshold = Threshold(now, box.IntParam(Unit), amount);

        return WiredQuantity.Passes(box, IsNegative, WiredVariableUse.Holders(box, run, box.IntParam(Target)), holder =>
        {
            if (run.Room.VariableTimes(variable, holder, run) is not { } times)
                return false;

            var stamp = box.IntParam(CompareValue) == CreationTime ? times.Created : times.Updated;
            // younger than the amount is "lower than" it
            return box.IntParam(Comparison) == WiredComparison.HigherThan ? stamp < threshold : stamp > threshold;
        });
    }

    /// <summary>The moment the amount of units before now. One reaching past the calendar's start (a box
    /// set to a million days, say) is its start: everything is younger than that.</summary>
    private static DateTimeOffset Threshold(DateTimeOffset now, int unit, int amount)
    {
        try
        {
            return unit switch
            {
                Milliseconds => now.AddMilliseconds(-amount),
                Seconds => now.AddSeconds(-amount),
                Minutes => now.AddMinutes(-amount),
                Hours => now.AddHours(-amount),
                Days => now.AddDays(-amount),
                Weeks => now.AddDays(-(double)amount * DaysPerWeek),
                Months => now.AddMonths(-amount),
                _ => now.AddYears(-amount),
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return amount > 0 ? DateTimeOffset.MinValue : DateTimeOffset.MaxValue;
        }
    }
}
