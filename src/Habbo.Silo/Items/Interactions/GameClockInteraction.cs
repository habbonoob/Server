using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A game timer or counter (the client's furniture_counter_clock). The room's games run
/// it; this says who may click it: those who control the room.</summary>
[FurniInteraction(Name)]
public sealed class GameClockInteraction : FurniInteraction
{
    public const string Name = "furniture_counter_clock";

    /// <summary>The click on its start/stop button (and a double-click).</summary>
    public const int StartStop = 1;

    /// <summary>The click on its reset button.</summary>
    public const int Reset = 2;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    /// <summary>A new countdown shows its first time (Habbo: a football counter placed as "30"); a counter that
    /// counts up, 0. Without it a counter that is only its digits drew nothing until started.</summary>
    public override void Grant(FurniGrant grant)
    {
        var first = grant.Definition.CustomParams.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(p => int.TryParse(p, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var s) && s > 0);
        FurniDataStore.Attach(grant.Db, grant.Item, FurniDataView.WithState(grant.ProductParam.Length > 0 ? grant.ProductParam : first ?? "0"));
    }
}
