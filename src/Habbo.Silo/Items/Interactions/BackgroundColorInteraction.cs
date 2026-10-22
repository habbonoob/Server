using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A background toner: colours the room's background while it is on. Its data is four
/// numbers — on, hue, saturation, lightness — and using it switches it on or off.</summary>
[FurniInteraction("furniture_background_color")]
public sealed class BackgroundColorInteraction : FurniInteraction
{
    // where each value sits in the item's numbers
    public const int OnIndex = 0;
    public const int HueIndex = 1;
    public const int SaturationIndex = 2;
    public const int LightnessIndex = 3;
    private const int ValueCount = 4;

    private const int Off = 0;
    private const int On = 1;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Use)
            return Task.CompletedTask;

        var values = Values(use.Item.Data);
        values[OnIndex] = values[OnIndex] == On ? Off : On;
        return use.Room.UpdateAsync(use.Item, WithValues(use.Item.Data, values));
    }

    /// <summary>The toner's numbers, filled out if it has never been set.</summary>
    public static List<int> Values(FurniDataView data)
    {
        var values = data.IntValues?.ToList() ?? [];
        while (values.Count < ValueCount)
            values.Add(Off);
        return values;
    }

    public static FurniDataView WithValues(FurniDataView data, List<int> values) =>
        data with { Type = (int)FurniDataType.IntArray, State = null, IntValues = values };
}
