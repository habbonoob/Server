# Catalog dumper

A [G-Earth](https://github.com/sirjonasxx/G-Earth) extension, built on
[xabbo/gearth](https://github.com/xabbo/gearth), that walks the live Habbo catalog and writes it
out as the hotel's seed catalog, `seed/catalog.json`: the catalog every new hotel starts with.
It can write SQL for a running hotel's `catalog_pages` / `catalog_offers` / `catalog_products`
tables instead.

It is deliberately **not** part of `Habbo.slnx`: it pulls four external dependencies that don't
belong in the server build.

## Running it

```
dotnet run --project tools/Habbo.CatalogDumper
```

Then in G-Earth: connect to Habbo, add this extension, and press the green play button to start
the dump. It walks the index tree one page at a time and writes `seed/catalog.json` when finished,
then says what it left out (below). Commit the file: it is the catalog new hotels are seeded with.

| Variable | Default | Meaning |
| --- | --- | --- |
| `CATALOG_OUTPUT` | `seed/catalog.json` | Where to write. A path ending in `.sql` writes SQL instead (`CATALOG_SQL_PATH` still works). |
| `CATALOG_FURNIDATA` | `seed/furnidata.xml` | The furnidata products are named from. |
| `CATALOG_TYPE` | `NORMAL` | Which catalog to walk (`NORMAL`, `BUILDERS_CLUB`). |
| `CATALOG_REQUEST_DELAY_MS` | `600` | Delay between page requests. |
| `CATALOG_AUTOSTART` | unset | `1` starts the dump as soon as the game is connected, without pressing play. |

`seed/...` paths are the repository's, found from the binary upwards.

**Leave the delay alone unless you have a reason.** The catalog is a few hundred pages and Habbo
rate-limits; requesting them back to back gets the account throttled and produces a half-empty
dump that still looks complete.

## Checking the output without Habbo

```
dotnet run --project tools/Habbo.CatalogDumper -- --sample        # the seed
dotnet run --project tools/Habbo.CatalogDumper -- --sample sql    # the SQL
```

Prints what a hand-built catalog becomes, covering the cases that are easy to get wrong: a nested
page, a bundle with several products, a coloured variant, a badge, an effect, an offer listed on
two pages, one priced in duckets, furniture furnidata lacks, a page that never answered, and text
containing an apostrophe. Useful for checking the output against the seeder or the schema after a
change to `CatalogSeedWriter` or `CatalogSqlWriter`.

## The seed

The seed names furniture by class name, read off furnidata by each product's class id (floor and
wall apart, as furnidata reuses ids between them), so **update `seed/furnidata.xml` first**
(`make gamedata`): an offer for furniture the hotel's furnidata lacks is left out, and the run
names the class ids it missed. Bot offers are left out too, as the hotel sells no bots. An effect
lasts a day once worn, as the shop's effects do; the catalog page doesn't say.

An offer listed on several pages is written in full where it first appears and as `{"id": n}`
after that; the seeder stores it once and lists it on each page. Each page is a line and each
offer a line, so a new dump's diff shows what Habbo changed.

The server seeds its catalog only into an empty catalog, so an existing hotel keeps its own. To
take a new seed there, empty it and restart (the listings, products and targeted offers cascade):

```sql
DELETE FROM catalog_front_page_items; DELETE FROM catalog_pages; DELETE FROM catalog_offers;
```

## Applying a dump as SQL

```
CATALOG_OUTPUT=catalog.sql dotnet run --project tools/Habbo.CatalogDumper
psql -U habbo -d habbo -v ON_ERROR_STOP=1 -f catalog.sql
```

**Import furnidata first.** Products are matched on `(ClassId, Type)` rather than by a definition
id, because `item_definitions.Id` is a surrogate generated at import time — it differs between
databases, and a furnidata class id on its own is ambiguous across the floor and wall sections.
Each product insert is therefore a `SELECT` that resolves the definition in the database:

```sql
INSERT INTO catalog_products ("OfferId","DefinitionId","ExtraParam","Amount")
SELECT 111, d."Id", '1', 1 FROM item_definitions d
WHERE d."ClassId" = 22 AND d."Type" = 0;
```

A product whose furniture was never imported inserts nothing rather than failing, so compare the
row counts against the header comment to see whether anything was dropped.

The script replaces the catalog wholesale: it runs in a transaction that starts by deleting
`catalog_pages` **and** `catalog_offers`. Both are roots — an offer no longer hangs off a page —
so clearing only one leaves the other behind and the first insert collides. `catalog_page_offers`
and `catalog_products` cascade from them. It also inserts page 1 as the tree's anchor, matching
`seed/catalog.json`: the server expects exactly one page with no parent.

Because it is one transaction, a failure rolls the whole thing back and leaves the existing
catalog untouched — so a bad dump cannot leave you with half a catalog.

## Hidden pages, and offers on several pages

Two things about Habbo's catalog that the output has to account for:

**Hidden pages arrive without their id.** Habbo sends `id = -1` for any page the client may not
open — pages that are disabled or hidden, which in practice are the ones that group others.
They are ordinary pages with real ids on Habbo's side; that id just never crosses the wire, so
we cannot know it.

They are still dumped: they hold the tree together, and their children name them as parent. But
`-1` cannot be stored, because every hidden page in the catalog would collide on a single row.
Each is instead given an id derived from its **name**, which is the one identity Habbo does send
and is unique catalog-wide (a US dump had 178 distinct names but only 163 distinct ids). The
derivation is a plain FNV-1a hash mapped below `-1`, so the same catalog always produces the same
ids and a re-dump does not renumber everything because one more page got hidden.

No page request is sent for them — asking for page `-1` just burns a full timeout and answers
nothing, which is what made the first dump take minutes longer than it needed to.

**An offer can be listed on many pages.** The same offer id, price and products appear under a
themed set, a category page and sometimes a game page — in a real US dump, 757 of 2,963 offers
were listed more than once. The offer and its products are therefore written once and
`catalog_page_offers` records each listing.

## Prices and order

**An offer's price is not just its credit price.** Habbo sells whole pages in duckets or
diamonds, and a few in silver. Those offers carry a credit price of `0`, so recording only
credits stores them as free — and the server will happily hand them over for nothing. The dump
records `PriceInActivityPoints` with its currency (`0` duckets, `1` seashell, `2` heart,
`3` gift point, `4` shell, `5` diamond) and `PriceInSilver` alongside.

The server holds no balance in any of those, so `PurchaseAsync` refuses a non-credit offer
outright rather than selling it for its credit price of zero.

**Offer order is per listing, not per offer.** The position lives on `catalog_page_offers`
because a cross-listed offer sits somewhere different on each page. Ordering by offer id instead
shuffles every page into an order the client never shows.

## What it captures

`ExtraParam` comes from the offer's product variant, which is the colour the client appends to
the asset name. Getting this wrong is not cosmetic: an item whose colours live under `class*n`
has no un-suffixed icon on the CDN, so a missing variant makes the catalog tile fail to load.
