using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Xabbo.Core;

namespace Habbo.CatalogDumper;

/// <summary>Turns a captured catalog into seed/catalog.json, the catalog a new hotel starts with: the same pages,
/// offers and listings as <see cref="CatalogSqlWriter"/>, in the seed's shape.
///
/// The seed names furniture by class name, which is what it looks definitions up by, so each product's class id is
/// read off furnidata here (floor and wall separately: furnidata reuses a class id between its two sections). An
/// offer listed on several pages is written in full where it first appears and by its id alone after that; the
/// seeder lists it again there. Offers the hotel can't sell are left out and counted: one naming furniture furnidata
/// lacks, and a bot, which the hotel's catalog has no product for.</summary>
public static class CatalogSeedWriter
{
    /// <summary>Habbo's own "no particular offer" value.</summary>
    private const int DefaultMaxAmount = 100;

    /// <summary>Our seed uses page 1 as the root; Habbo's own ids start above it.</summary>
    private const int RootPageId = 1;

    /// <summary>A catalog page doesn't say how long an effect lasts; the shop's effects wear off after a day.</summary>
    private const int EffectSeconds = 24 * 60 * 60;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>What was left out, for the run's report.</summary>
    public sealed record Omitted(int UnknownFurni, int Bots, IReadOnlyCollection<int> UnknownClassIds);

    /// <summary>Furnidata's class names by class id and section, the way products name furniture.</summary>
    public static IReadOnlyDictionary<(int ClassId, bool Wall), string> ReadFurnidata(string path)
    {
        var classes = new Dictionary<(int, bool), string>();
        var root = XDocument.Load(path).Root!;
        foreach (var (section, wall) in new[] { ("roomitemtypes", false), ("wallitemtypes", true) })
        {
            foreach (var furni in root.Element(section)?.Elements("furnitype") ?? [])
            {
                if (int.TryParse((string?)furni.Attribute("id"), out var id) && (string?)furni.Attribute("classname") is { } name)
                    classes[(id, wall)] = name;
            }
        }
        return classes;
    }

    public static (string Json, Omitted Omitted) Write(
        CatalogSnapshot snapshot, IReadOnlyDictionary<(int ClassId, bool Wall), string> furnidata)
    {
        var written = new HashSet<int>();
        var unknownClasses = new HashSet<int>();
        var (unknownFurni, bots) = (0, 0);

        var pages = new List<string>
        {
            // the tree's anchor, never rendered: the server expects exactly one page with no parent
            Page(new { id = RootPageId, parentId = (int?)null, pageName = "root", localization = "", icon = 0, visible = true,
                layoutCode = "", orderIndex = 0, images = Array.Empty<string>(), texts = Array.Empty<string>() }, []),
        };

        foreach (var page in snapshot.Pages)
        {
            var offers = new List<string>();
            var listed = new HashSet<int>();
            foreach (var offer in page.Offers)
            {
                // a page can name the same offer twice; it is listed once
                if (!listed.Add(offer.Id))
                    continue;
                if (written.Contains(offer.Id))
                {
                    offers.Add(JsonSerializer.Serialize(new { id = offer.Id }, Json));
                    continue;
                }

                if (offer.Products.Any(p => p.Type == ItemType.Bot))
                {
                    bots++;
                    continue;
                }
                var unknown = offer.Products
                    .Where(p => p.Type is ItemType.Floor or ItemType.Wall && !furnidata.ContainsKey((p.Kind, p.Type == ItemType.Wall)))
                    .ToList();
                if (unknown.Count > 0)
                {
                    unknownFurni++;
                    unknownClasses.UnionWith(unknown.Select(p => p.Kind));
                    continue;
                }

                written.Add(offer.Id);
                offers.Add(JsonSerializer.Serialize(Offer(offer, furnidata), Json));
            }

            pages.Add(Page(new
            {
                id = page.Id,
                parentId = page.ParentId ?? RootPageId,
                pageName = page.Node.Name,
                localization = page.Node.Title,
                icon = page.Node.Icon,
                visible = page.Node.IsVisible,
                layoutCode = page.LayoutCode,
                orderIndex = page.OrderIndex,
                images = page.Images,
                texts = page.Texts,
            }, offers));
        }

        var json = new StringBuilder();
        json.Append("{\n \"capturedFrom\": ").Append(JsonSerializer.Serialize(
            $"Habbo's {snapshot.CatalogType} catalog, {DateTimeOffset.UtcNow:yyyy-MM-dd}; tools/Habbo.CatalogDumper", Json));
        json.Append(",\n \"pages\": [\n").AppendJoin(",\n", pages).Append("\n ]\n}\n");
        return (json.ToString(), new Omitted(unknownFurni, bots, unknownClasses));
    }

    /// <summary>A page on a line of its own, then its offers a line each, so a change to the catalog reads as one.</summary>
    private static string Page(object page, IReadOnlyList<string> offers)
    {
        var head = JsonSerializer.Serialize(page, Json);
        var body = offers.Count == 0 ? "[]" : "[\n    " + string.Join(",\n    ", offers) + "\n   ]";
        return "  " + head[..^1] + ",\"offers\":" + body + "}";
    }

    /// <summary>Every price the offer carries, not just the credit one: Habbo sells whole pages in duckets or diamonds,
    /// and an offer recorded with its credit price alone would be free.</summary>
    private static object Offer(ICatalogOffer offer, IReadOnlyDictionary<(int ClassId, bool Wall), string> furnidata)
    {
        // PriceInSilver is on the concrete type rather than the interface
        var silver = offer is CatalogOffer concrete ? concrete.PriceInSilver : 0;
        var limited = offer.Products.Where(p => p.IsLimited).Select(p => p.LimitedTotal).DefaultIfEmpty(0).Max();
        return new
        {
            id = offer.Id,
            localizationId = offer.FurniLine,
            priceInCredits = offer.PriceInCredits,
            priceInActivityPoints = offer.PriceInActivityPoints == 0 ? (int?)null : offer.PriceInActivityPoints,
            activityPointType = offer.PriceInActivityPoints == 0 ? null : offer.ActivityPointType.ToString(),
            priceInSilver = silver == 0 ? (int?)null : silver,
            clubLevel = offer.ClubLevel switch { 1 => "Club", 2 => "Vip", _ => null },
            maxAmount = offer.CanPurchaseMultiple ? DefaultMaxAmount : 1,
            isPet = offer.IsPet ? true : (bool?)null,
            giftable = offer.CanPurchaseAsGift,
            isRentable = offer.IsRentable ? true : (bool?)null,
            previewImage = string.IsNullOrEmpty(offer.PreviewImage) ? null : offer.PreviewImage,
            limitedStack = limited == 0 ? (int?)null : limited,
            products = offer.Products.Select(product => Product(product, furnidata)).ToList(),
        };
    }

    /// <summary>Furniture by class name, a badge by its code, an effect by its id.</summary>
    private static object Product(ICatalogProduct product, IReadOnlyDictionary<(int ClassId, bool Wall), string> furnidata) =>
        product.Type switch
        {
            ItemType.Badge => new { type = "Badge", extraParam = product.Variant, amount = Math.Max(1, product.Count) },
            ItemType.Effect => new { type = "Effect", classId = product.Kind, durationSeconds = EffectSeconds, amount = Math.Max(1, product.Count) },
            _ => (object)new
            {
                className = furnidata[(product.Kind, product.Type == ItemType.Wall)],
                extraParam = string.IsNullOrEmpty(product.Variant) ? null : product.Variant,
                amount = Math.Max(1, product.Count),
            },
        };
}
