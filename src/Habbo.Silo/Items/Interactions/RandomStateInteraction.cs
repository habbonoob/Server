using System.Globalization;
using Habbo.Domain.Rooms;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Furniture that lands on a random state when used, such as a fortune wheel. Its
/// customparams say how many outcomes it has and how long a spin lasts, e.g.
/// "states=8,delay=2000"; without them the outcomes are the item's states from 1 upwards.
/// Further spins are ignored until the current one has had its time.</summary>
[FurniInteraction("furniture_randomstate")]
public sealed class RandomStateInteraction : FurniInteraction
{
    private const string StatesParam = "states";
    private const string DelayParam = "delay";
    private const int FirstOutcome = 1;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.UseRandom || use.Item.IsBusy)
            return Task.CompletedTask;

        var definition = use.Item.Item.Definition!;
        var parameters = Parse(definition.CustomParams);

        var outcomes = parameters.TryGetValue(StatesParam, out var count)
            ? Enumerable.Range(FirstOutcome, Math.Max(0, count)).ToList()
            : definition.States.Select(s => s.State).Where(state => state >= FirstOutcome).Order().ToList();
        if (outcomes.Count == 0)
            return Task.CompletedTask;

        if (parameters.TryGetValue(DelayParam, out var delay))
            use.Item.BusyUntil = DateTime.UtcNow + TimeSpan.FromMilliseconds(delay);

        var outcome = outcomes[Random.Shared.Next(outcomes.Count)];
        return use.Room.UpdateAsync(use.Item, use.Item.Data with { State = outcome.ToString(CultureInfo.InvariantCulture) });
    }

    /// <summary>Reads "key=value,key=value", keeping the pairs whose value is a number.</summary>
    private static Dictionary<string, int> Parse(string customParams) =>
        customParams
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(pair => pair.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(pair => pair.Length == 2
                           && int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .DistinctBy(pair => pair[0])
            .ToDictionary(pair => pair[0], pair => int.Parse(pair[1], CultureInfo.InvariantCulture), StringComparer.Ordinal);
}
