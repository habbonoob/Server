using System.Globalization;
using System.Text;
using Xabbo.Core;

namespace Habbo.CatalogDumper;

/// <summary>Turns a captured catalog into SQL for the emulator's catalog tables.
///
/// Products are resolved by (ClassId, Type) rather than by a definition id, because our
/// item_definitions primary key is a surrogate generated at import time — it differs between
/// databases, and furnidata's class id alone is ambiguous across the floor and wall sections.
/// The insert therefore looks the definition up rather than naming its key.</summary>
public static class CatalogSqlWriter
{
    /// <summary>Habbo's own "no particular offer" value.</summary>
    private const int DefaultMaxAmount = 100;

    public static string Write(CatalogSnapshot snapshot)
    {
        var sql = new StringBuilder();

        // An offer listed on several pages is written once; the link is what repeats.
        var writtenOffers = new HashSet<int>();

        // ...but a page can also list the same offer twice, and the link table is keyed on the
        // pair, so the repeat has to be dropped rather than written again.
        var writtenListings = new HashSet<(int Page, int Offer)>();

        sql.AppendLine($"-- Habbo catalog dump ({snapshot.CatalogType})");
        sql.AppendLine($"-- captured {DateTimeOffset.UtcNow:u}");
        sql.AppendLine($"-- {snapshot.Pages.Count} page(s), {snapshot.DistinctOfferCount} offer(s) " +
                       $"in {snapshot.OfferCount} listing(s), {snapshot.ProductCount} product(s)");
        sql.AppendLine("--");
        sql.AppendLine("-- Replaces the catalog wholesale. Furniture is matched on (ClassId, Type),");
        sql.AppendLine("-- so run the furnidata import first or offers will be dropped as unmatched.");
        sql.AppendLine();
        sql.AppendLine("BEGIN;");
        sql.AppendLine();
        // Offers no longer hang off a page, so clearing pages alone leaves every old offer
        // behind and the first insert collides on its primary key. Both roots must go; the
        // link table and the products cascade from them.
        sql.AppendLine("-- catalog_page_offers and catalog_products cascade from these.");
        sql.AppendLine("DELETE FROM catalog_pages;");
        sql.AppendLine("DELETE FROM catalog_offers;");
        sql.AppendLine();

        // A single root anchors the tree; the client never renders it, but our tree builder
        // expects exactly one page with no parent.
        sql.AppendLine("-- The tree's anchor. Never rendered.");
        sql.AppendLine(PageInsert(
            id: RootPageId, parentId: null, pageName: "root", localization: "",
            icon: 0, visible: true, layoutCode: "", orderIndex: 0, images: [], texts: []));
        sql.AppendLine();

        foreach (var page in snapshot.Pages)
        {
            sql.AppendLine(PageInsert(
                page.Id,
                page.ParentId ?? RootPageId,
                page.Node.Name,
                page.Node.Title,
                page.Node.Icon,
                page.Node.IsVisible,
                page.LayoutCode,
                page.OrderIndex,
                page.Images,
                page.Texts));

            var position = 0;

            foreach (var offer in page.Offers)
            {
                // Habbo lists one offer under several pages — the same id, price and products
                // under a themed set, a category and sometimes a game page. The offer and its
                // products are written once; only the link is written per page.
                if (writtenOffers.Add(offer.Id))
                {
                    sql.AppendLine("  " + OfferInsert(offer));

                    foreach (var product in offer.Products)
                        sql.AppendLine("  " + ProductInsert(product, offer.Id));
                }

                if (writtenListings.Add((page.Id, offer.Id)))
                    sql.AppendLine("  " + ListingInsert(page.Id, offer.Id, position++));
            }

            sql.AppendLine();
        }

        // Both ids came from Habbo, so the sequences behind them are untouched. Without this
        // the next page or offer created in the hotel is handed id 1 and collides immediately.
        // GREATEST guards the floor, because a hidden page's derived id is negative.
        sql.AppendLine("-- Let the hotel create its own pages and offers after a dump.");
        sql.AppendLine("""
            SELECT setval(pg_get_serial_sequence('catalog_pages', 'Id'),
                GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_pages), 1));
            SELECT setval(pg_get_serial_sequence('catalog_offers', 'Id'),
                GREATEST((SELECT COALESCE(MAX("Id"), 0) FROM catalog_offers), 1));
            """);
        sql.AppendLine();
        sql.AppendLine("COMMIT;");
        return sql.ToString();
    }

    /// <summary>Our seed uses page 1 as the root; Habbo's own ids start above it.</summary>
    private const int RootPageId = 1;

    private static string PageInsert(
        int id, int? parentId, string pageName, string localization, int icon, bool visible,
        string layoutCode, int orderIndex, IReadOnlyList<string> images, IReadOnlyList<string> texts) =>
        "INSERT INTO catalog_pages " +
        "(\"Id\",\"ParentId\",\"PageName\",\"Localization\",\"Icon\",\"Visible\",\"LayoutCode\"," +
        "\"Images\",\"Texts\",\"MinRank\",\"OrderIndex\") VALUES (" +
        $"{id},{(parentId is null ? "NULL" : parentId.ToString())},{Quote(pageName)},{Quote(localization)}," +
        $"{icon},{Bool(visible)},{Quote(layoutCode)},{Array(images)},{Array(texts)},1,{orderIndex});";

    /// <summary>Every price the offer carries, not just the credit one. Habbo sells whole
    /// pages in duckets or diamonds; recording only the credit price stores those as free.</summary>
    private static string OfferInsert(ICatalogOffer offer)
    {
        // PriceInSilver is on the concrete type rather than the interface.
        var silver = offer is CatalogOffer concrete ? concrete.PriceInSilver : 0;

        return "INSERT INTO catalog_offers " +
               "(\"Id\",\"LocalizationId\",\"PriceInCredits\",\"PriceInActivityPoints\"," +
               "\"ActivityPointType\",\"PriceInSilver\",\"IsPet\",\"Giftable\",\"IsRentable\"," +
               "\"PreviewImage\",\"ClubLevel\",\"MaxAmount\") VALUES (" +
               $"{offer.Id},{Quote(offer.FurniLine)},{offer.PriceInCredits}," +
               $"{offer.PriceInActivityPoints},{(int)offer.ActivityPointType},{silver}," +
               $"{Bool(offer.IsPet)},{Bool(offer.CanPurchaseAsGift)},{Bool(offer.IsRentable)}," +
               $"{Quote(offer.PreviewImage)},{offer.ClubLevel}," +
               $"{(offer.CanPurchaseMultiple ? DefaultMaxAmount : 1)});";
    }

    /// <summary>One page listing one offer, at the position Habbo listed it. The position is a
    /// property of the listing: a cross-listed offer sits somewhere different on each page.</summary>
    private static string ListingInsert(int pageId, int offerId, int orderIndex) =>
        "INSERT INTO catalog_page_offers (\"PageId\",\"OfferId\",\"OrderIndex\") VALUES (" +
        $"{pageId},{offerId},{orderIndex});";

    /// <summary>A badge has no furniture definition, so it is inserted directly. Furniture is
    /// inserted from a SELECT so the definition's surrogate key is resolved in the database.
    /// An offer naming furniture we never imported inserts nothing, which is why the dump
    /// reports the counts it expected.</summary>
    private static string ProductInsert(ICatalogProduct product, int offerId)
    {
        const string columns =
            "INSERT INTO catalog_products (\"OfferId\",\"DefinitionId\",\"ExtraParam\",\"Amount\")";

        if (product.Type == ItemType.Badge)
        {
            return $"{columns} VALUES " +
                   $"({offerId},NULL,{Quote(product.Variant)},{Math.Max(1, product.Count)});";
        }

        return $"{columns} SELECT {offerId}, d.\"Id\", {Quote(product.Variant)}, " +
               $"{Math.Max(1, product.Count)} FROM item_definitions d " +
               $"WHERE d.\"ClassId\" = {product.Kind} AND d.\"Type\" = {TypeValue(product.Type)};";
    }

    /// <summary>Matches the ItemType enum's stored values: Floor 0, Wall 1.</summary>
    private static int TypeValue(ItemType type) => type == ItemType.Wall ? 1 : 0;

    private static string Bool(bool value) => value ? "TRUE" : "FALSE";

    /// <summary>Postgres text[] literal, built from quoted elements so a value containing a
    /// comma or a brace cannot break out of the array.</summary>
    private static string Array(IReadOnlyList<string> values) =>
        values.Count == 0
            ? "'{}'"
            : "ARRAY[" + string.Join(",", values.Select(Quote)) + "]::text[]";

    private static string Quote(string? value) =>
        value is null ? "NULL" : "'" + value.Replace("'", "''") + "'";
}
