using System.Globalization;
using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Furni;
using Microsoft.EntityFrameworkCore;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A trophy: engraved with the buyer's name, the date and a message when it is bought.
/// The engraving is stored as those three things in furni_trophies; the client's trophy widget
/// wants them as one tab-separated legacy string, which is built only when the item is loaded.</summary>
[FurniInteraction("furniture_trophy")]
public sealed class TrophyInteraction : FurniInteraction, IFurniDataOwner
{
    /// <summary>What the client's trophy widget splits the string on.</summary>
    private const char Separator = '\t';

    /// <summary>How the engraving date is written on the trophy.</summary>
    private const string DateFormat = "dd-MM-yyyy";

    public override void Grant(FurniGrant grant)
    {
        grant.Item.DataType = FurniDataType.Legacy;
        grant.Db.FurniTrophies.Add(new FurniTrophy
        {
            Item = grant.Item,
            EngraverName = grant.BuyerName,
            EngravedAt = grant.At,
            Message = Engraving(grant.ClientParam),
        });
    }

    public async Task LoadAsync(
        AppDbContext db, IReadOnlyCollection<UserItem> items, IDictionary<int, FurniDataView> views, CancellationToken ct)
    {
        var ids = items.Select(item => item.Id).ToList();
        var trophies = await db.FurniTrophies.AsNoTracking()
            .Where(t => ids.Contains(t.ItemId))
            .ToListAsync(ct);

        foreach (var trophy in trophies)
        {
            var text = string.Join(Separator,
                trophy.EngraverName,
                trophy.EngravedAt.ToString(DateFormat, CultureInfo.InvariantCulture),
                trophy.Message);
            views[trophy.ItemId] = views[trophy.ItemId] with { Type = FurniDataView.LegacyType, State = text };
        }
    }

    /// <summary>The buyer's text as it can be engraved: a tab would split it in the client's
    /// widget, and the column has a limit.</summary>
    private static string Engraving(string text)
    {
        var cleaned = text.Replace(Separator, ' ').Trim();
        return cleaned.Length > FurniTrophy.MaxMessageLength ? cleaned[..FurniTrophy.MaxMessageLength] : cleaned;
    }
}
