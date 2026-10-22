using System.Globalization;
using Habbo.Domain.Furniture.Data;
using Habbo.Domain.Groups;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items;

/// <summary>Group furniture's data, as the client's FurnitureGuildCustomizedLogic reads it: a string array of the
/// item's state, its group's id, the group's badge and its two colours (hex). Only the state and the group are the
/// item's own; the badge and colours are the group's, filled in each time the data is read, so an edited group's
/// furniture follows it.</summary>
public static class GroupFurniData
{
    public const int State = 0, Group = 1, Badge = 2, PrimaryColor = 3, SecondaryColor = 4;

    public const string FirstState = "0";

    /// <summary>New data for an item of this group (none: of no group), its badge and colours left to be filled in.</summary>
    public static FurniDataView Of(string state, int? groupId) => FurniDataView.Plain with
    {
        Type = (int)FurniDataType.StringArray,
        StringValues = [state, (groupId ?? 0).ToString(CultureInfo.InvariantCulture), "", "", ""],
    };

    /// <summary>The data with another state, the rest as it was.</summary>
    public static FurniDataView WithState(FurniDataView data, string state)
    {
        var values = (data.StringValues ?? []).ToList();
        while (values.Count <= SecondaryColor)
            values.Add("");
        values[State] = state;
        return data with { StringValues = values };
    }

    /// <summary>Fills in the badge and colours of each group item's group, as the group is now.</summary>
    public static async Task FillAsync(AppDbContext db, IReadOnlyDictionary<int, int> groupOfItem, Dictionary<int, FurniDataView> views,
        CancellationToken ct = default)
    {
        if (groupOfItem.Count == 0)
            return;

        var groupIds = groupOfItem.Values.Distinct().ToList();
        var groups = await db.Groups.AsNoTracking().Where(g => groupIds.Contains(g.Id))
            .Select(g => new { g.Id, g.BadgeCode, g.PrimaryColorId, g.SecondaryColorId })
            .ToDictionaryAsync(g => g.Id, ct);
        var colors = await db.GroupColors.AsNoTracking().Where(c => c.Kind != GroupColorKind.Badge).ToListAsync(ct);
        string Hex(GroupColorKind kind, int id) => colors.FirstOrDefault(c => c.Kind == kind && c.Id == id)?.Hex ?? string.Empty;

        foreach (var (itemId, groupId) in groupOfItem)
        {
            if (!views.TryGetValue(itemId, out var view) || view.StringValues is null || !groups.TryGetValue(groupId, out var group))
                continue;

            var values = view.StringValues.ToList();
            while (values.Count <= SecondaryColor)
                values.Add("");
            values[Group] = groupId.ToString(CultureInfo.InvariantCulture);
            values[Badge] = group.BadgeCode;
            values[PrimaryColor] = Hex(GroupColorKind.Primary, group.PrimaryColorId);
            values[SecondaryColor] = Hex(GroupColorKind.Secondary, group.SecondaryColorId);
            views[itemId] = view with { StringValues = values };
        }
    }
}
