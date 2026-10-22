using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items;

/// <summary>The data of furniture that shows a badge with an engraving under it, a badge display case or a resolution
/// statue: its state, the badge, whose it is and the date (StringArrayStuffData, read by FurnitureBadgeDisplayWidgetHandler).</summary>
public static class BadgeEngraving
{
    /// <summary>How the date is written, as on a trophy.</summary>
    public const string DateFormat = "dd-MM-yyyy";

    public static FurniDataView Of(int state, string badge, string owner, DateTimeOffset? at) => FurniDataView.Plain with
    {
        Type = (int)FurniDataType.StringArray,
        State = null,
        StringValues =
        [
            state.ToString(CultureInfo.InvariantCulture), badge, owner,
            at?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? string.Empty,
        ],
    };

    /// <summary>No badge and nothing engraved yet.</summary>
    public static FurniDataView Blank(int state) => Of(state, string.Empty, string.Empty, null);

    /// <summary>The state the data holds; data of another layout counts as <paramref name="otherwise"/>.</summary>
    public static int StateOf(FurniDataView data, int otherwise) =>
        data.Type == (int)FurniDataType.StringArray && data.StringValues is [var state, ..]
                                                    && int.TryParse(state, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : otherwise;
}
