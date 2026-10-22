using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A room area hider: while on, it hides the furniture in a rectangle of the room from view.
/// Its data is eight numbers — on, the rectangle's corner, its width and length, whether the hidden
/// furni are invisible, whether wall items are hidden too, and whether the area is inverted.
/// Using it switches it on or off.</summary>
[FurniInteraction("furniture_area_hide")]
public sealed class AreaHideInteraction : FurniInteraction
{
    // where each value sits in the item's numbers
    public const int OnIndex = 0;
    public const int RootXIndex = 1;
    public const int RootYIndex = 2;
    public const int WidthIndex = 3;
    public const int LengthIndex = 4;
    public const int InvisibilityIndex = 5;
    public const int WallItemsIndex = 6;
    public const int InvertIndex = 7;
    private const int ValueCount = 8;

    public const int False = 0;
    public const int True = 1;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Use)
            return Task.CompletedTask;

        var values = Values(use.Item.Data);
        values[OnIndex] = values[OnIndex] == True ? False : True;
        return use.Room.UpdateAsync(use.Item, WithValues(use.Item.Data, values));
    }

    public static List<int> Values(FurniDataView data)
    {
        var values = data.IntValues?.ToList() ?? [];
        while (values.Count < ValueCount)
            values.Add(False);
        return values;
    }

    public static FurniDataView WithValues(FurniDataView data, List<int> values) =>
        data with { Type = (int)FurniDataType.IntArray, State = null, IntValues = values };

    public static AreaHideData Area(int itemId, FurniDataView data)
    {
        var values = Values(data);
        return new AreaHideData(itemId, values[OnIndex] == True, values[RootXIndex], values[RootYIndex],
            values[WidthIndex], values[LengthIndex], values[InvertIndex] == True);
    }
}
