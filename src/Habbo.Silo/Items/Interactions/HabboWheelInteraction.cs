using System.Globalization;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A wheel of fortune: spun, it turns (state -1) and stops on one of its ten numbers. The
/// client's wheel animates exactly those, so they are its outcomes; further spins wait for this one.</summary>
[FurniInteraction("furniture_habbowheel")]
public sealed class HabboWheelInteraction : FurniInteraction
{
    private const string Spinning = "-1";
    private const int FirstNumber = 1;
    private const int Numbers = 10;

    /// <summary>How long the client's wheel takes to slow down and stop.</summary>
    private static readonly TimeSpan SpinTime = TimeSpan.FromSeconds(3);

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override async Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Spin || use.Item.IsBusy)
            return;

        use.Item.BusyUntil = DateTime.UtcNow + SpinTime;
        // spinning is only shown: picked up mid-spin, the wheel keeps the number it had rather than spinning for ever after
        await use.Room.UpdateAsync(use.Item, use.Item.Data with { State = Spinning }, persist: false);

        var number = Random.Shared.Next(FirstNumber, FirstNumber + Numbers);
        use.Room.Schedule(SpinTime, () => use.Room.IsPlaced(use.Item)
            ? use.Room.UpdateAsync(use.Item, use.Item.Data with { State = number.ToString(CultureInfo.InvariantCulture) })
            : Task.CompletedTask);
    }
}
