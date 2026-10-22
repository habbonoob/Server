using System.Globalization;
using Habbo.Domain.Rooms;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Furniture that steps through its states on use: a lamp goes on and off, a curtain
/// opens and closes, a wired switch flips. The states are the ones the item's SWF can draw, in
/// order, wrapping back to the first after the last.</summary>
[FurniInteraction("furniture_multistate", "furniture_window")]
public sealed class MultiStateInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        var data = use.Item.Data;
        if (use.Action != FurniAction.Use || data.Type != FurniDataView.LegacyType)
            return Task.CompletedTask;

        // Negative states are transient looks some items have, such as a dice rolling; they
        // are never where a click should land.
        var states = use.Item.Item.Definition!.States
            .Select(s => s.State)
            .Where(state => state >= 0)
            .Order()
            .ToList();
        if (states.Count < 2)
            return Task.CompletedTask;

        var current = int.TryParse(data.State, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
        var next = states[(states.IndexOf(current) + 1) % states.Count];

        return use.Room.UpdateAsync(use.Item, data with { State = next.ToString(CultureInfo.InvariantCulture) });
    }
}
