using Xabbo.Core;

namespace Habbo.CatalogDumper;

/// <summary>A hand-built snapshot used by <c>--sample</c> to print the SQL this tool would
/// produce, without needing a live Habbo connection. It covers the cases that are easy to get
/// wrong: a nested page, a bundle with several products, a coloured variant, a badge, and text
/// containing a quote.</summary>
public static class SampleSnapshot
{
    public static CatalogSnapshot Build() => new("NORMAL", [
        new CapturedPage(
            new Node(10, "seating", "Seating", Icon: 8),
            new Page(10, "default_3x3",
                Images: ["seating.png"],
                Texts: ["Seating", "Somewhere to sit down"],
                Offers: [
                    new Offer(100, "chair_silo", PriceInCredits: 5, Products: [
                        new Product(ItemType.Floor, Kind: 26, Variant: "", Count: 1),
                    ]),
                    // A coloured variant: the variant is what the client appends to the asset
                    // name, and an item whose colours live under "class*n" has no bare icon.
                    new Offer(111, "table_plasto_4leg", PriceInCredits: 9, Products: [
                        new Product(ItemType.Floor, Kind: 22, Variant: "1", Count: 1),
                    ]),
                ]))
        { Id = 10, ParentId = null, OrderIndex = 0 },

        new CapturedPage(
            new Node(14, "bundles", "Bundles & Badges", Icon: 30),
            new Page(14, "default_3x3",
                Images: [],
                // An apostrophe has to survive into the SQL literal.
                Texts: ["Habbo's bundles"],
                Offers: [
                    new Offer(140, "bundle_dining", PriceInCredits: 20, Products: [
                        new Product(ItemType.Floor, Kind: 22, Variant: "1", Count: 1),
                        new Product(ItemType.Floor, Kind: 26, Variant: "", Count: 4),
                    ]),
                    new Offer(150, "badge_offer", PriceInCredits: 3, Products: [
                        new Product(ItemType.Badge, Kind: 0, Variant: "ACH_Login1", Count: 1),
                    ]),
                    // Priced in duckets, not credits. Recording only the credit price would
                    // store this as free.
                    new Offer(160, "ducket_rug", PriceInCredits: 0, Products: [
                        new Product(ItemType.Floor, Kind: 59, Variant: "", Count: 1),
                    ]) { PriceInActivityPoints = 40, ActivityPointType = (ActivityPointType)0 },
                    // The same offer as on the seating page. Habbo does this constantly: it
                    // must be stored once and linked twice, not inserted twice.
                    new Offer(100, "chair_silo", PriceInCredits: 5, Products: [
                        new Product(ItemType.Floor, Kind: 26, Variant: "", Count: 1),
                    ]),
                    // An effect is named by its id, not by furniture.
                    new Offer(170, "fx_skates", PriceInCredits: 10, Products: [
                        new Product(ItemType.Effect, Kind: 8, Variant: "", Count: 1),
                    ]),
                    // Furniture furnidata doesn't have: the seed leaves the offer out.
                    new Offer(180, "unreleased", PriceInCredits: 1, Products: [
                        new Product(ItemType.Floor, Kind: 999999, Variant: "", Count: 1),
                    ]),
                ]))
        { Id = 14, ParentId = 10, OrderIndex = 0 },

        // A page whose id Habbo hid: it gets one derived from its name. It still needs a row,
        // or its children are orphaned.
        new CapturedPage(new Node(HiddenPageIdentity.HiddenPageId, "quiet", "Quiet Folder", Icon: 1), Page: null)
        { Id = HiddenPageIdentity.Derive("quiet", []), ParentId = null, OrderIndex = 1, IsFolder = true },
    ]);

    /// <summary>The furnidata the sample's products are named from.</summary>
    public static IReadOnlyDictionary<(int ClassId, bool Wall), string> Furnidata { get; } =
        new Dictionary<(int ClassId, bool Wall), string>
        {
            [(26, false)] = "chair_silo",
            [(22, false)] = "table_plasto_4leg*1",
            [(59, false)] = "rug_ducket",
        };

    private sealed record Node(int Id, string Name, string Title, int Icon) : ICatalogPageNode
    {
        public ICatalog? Catalog => null;
        public ICatalogPageNode? Parent => null;
        public bool IsVisible => true;
        public IReadOnlyList<int> OfferIds => [];
        public IReadOnlyList<ICatalogPageNode> Children => [];
        public ICatalogPageNode? FindNode(Func<ICatalogPageNode, bool> predicate) => null;
        public ICatalogPageNode? FindNode(string? title = null, string? name = null, int? id = null) => null;
        public IEnumerable<ICatalogPageNode> EnumerateDescendantsAndSelf() => [this];
    }

    private sealed record Page(
        int Id, string LayoutCode, IReadOnlyList<string> Images, IReadOnlyList<string> Texts,
        IReadOnlyList<ICatalogOffer> Offers) : ICatalogPage
    {
        public string CatalogType => "NORMAL";
        public bool AcceptSeasonCurrencyAsCredits => false;
        public IReadOnlyList<ICatalogPageItem> Data => [];
    }

    private sealed record Offer(
        int Id, string FurniLine, int PriceInCredits, IReadOnlyList<ICatalogProduct> Products) : ICatalogOffer
    {
        public ICatalogPage? Page => null;
        public bool IsRentable => false;
        public int PriceInActivityPoints { get; init; }
        public ActivityPointType ActivityPointType { get; init; }
        public bool CanPurchaseAsGift => false;
        public int ClubLevel => 0;
        public bool CanPurchaseMultiple => true;
        public bool IsPet => false;
        public string PreviewImage => "";
    }

    private sealed record Product(ItemType Type, int Kind, string Variant, int Count) : ICatalogProduct
    {
        public bool IsLimited => false;
        public int LimitedTotal => 0;
        public int LimitedRemaining => 0;
        public Xabbo.Id Id => -1;
        public string? Identifier => null;
        public bool IsFloorItem => Type == ItemType.Floor;
        public bool IsWallItem => Type == ItemType.Wall;
    }
}
