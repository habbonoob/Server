using Habbo.CatalogDumper;
using Xabbo.GEarth;

// A G-Earth extension that walks the live Habbo catalog and writes it out as the hotel's seed catalog
// (seed/catalog.json), or as SQL for a running hotel's catalog tables. Connect G-Earth to Habbo, start this
// extension, then press the green play button to begin the dump.

var catalogType = Environment.GetEnvironmentVariable("CATALOG_TYPE") ?? "NORMAL";
var outputPath = Environment.GetEnvironmentVariable("CATALOG_OUTPUT")
                 ?? Environment.GetEnvironmentVariable("CATALOG_SQL_PATH")
                 ?? FromRepository("seed/catalog.json");
var asSql = outputPath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase);
var furnidataPath = Environment.GetEnvironmentVariable("CATALOG_FURNIDATA") ?? FromRepository("seed/furnidata.xml");
var delayMs = int.TryParse(Environment.GetEnvironmentVariable("CATALOG_REQUEST_DELAY_MS"), out var d) ? d : 600;

// Starts the dump as soon as the game is connected, for running without anyone at G-Earth to press play.
var autostart = Environment.GetEnvironmentVariable("CATALOG_AUTOSTART") is "1" or "true";

// --sample prints what this tool produces from a hand-built catalog (--sample sql: the SQL), so the output can be
// checked against the seeder or the schema without a live connection.
if (args.Contains("--sample"))
{
    Console.Write(args.Contains("sql")
        ? CatalogSqlWriter.Write(SampleSnapshot.Build())
        : CatalogSeedWriter.Write(SampleSnapshot.Build(), SampleSnapshot.Furnidata).Json);
    return;
}

// read before the walk, so a missing furnidata fails now rather than after minutes of paging
var furnidata = asSql ? null : CatalogSeedWriter.ReadFurnidata(furnidataPath);

// --capture <file.jsonl>: the catalog the web client captured through the relay (habbo-pixi asked for the pages
// itself), written as a walk would write it; no G-Earth
if (Array.IndexOf(args, "--capture") is var at and >= 0 && at + 1 < args.Length)
{
    var captured = CaptureReplay.Read(args[at + 1], catalogType);
    CatalogSeedWriter.Omitted? left = null;
    if (asSql)
        await File.WriteAllTextAsync(outputPath, CatalogSqlWriter.Write(captured));
    else
    {
        (var text, left) = CatalogSeedWriter.Write(captured, furnidata!);
        await File.WriteAllTextAsync(outputPath, text);
    }
    Console.WriteLine($"{captured.Pages.Count} page(s), {captured.DistinctOfferCount} offer(s) to {outputPath}.");
    if (left is not null)
        Console.WriteLine(left);
    return;
}

var ext = new GEarthExtension(new GEarthOptions
{
    Name = "Catalog dumper",
    Description = "Dumps the Habbo catalog to SQL",
    Author = "habbo-emulator",
    Version = "1.0",
});

var running = 0;

ext.Connected += e =>
{
    Console.WriteLine(autostart
        ? $"Connected to {e.Session.Hotel}. Dumping the {catalogType} catalog."
        : $"Connected to {e.Session.Hotel}. Press play to dump the catalog.");
    if (autostart)
        _ = DumpAsync();
};
ext.Disconnected += () => Console.WriteLine("Game disconnected.");


ext.Activated += () => _ = DumpAsync();

await ext.RunAsync();

Task DumpAsync() => Task.Run(async () =>
{
    // The walk takes minutes; a second press while one is in flight would interleave two sets
    // of requests onto the same capture and mix their responses together.
    if (Interlocked.Exchange(ref running, 1) == 1)
    {
        Console.WriteLine("A dump is already running.");
        return;
    }

    try
    {
        var walker = new CatalogWalker(ext, delayMs);
        var snapshot = await walker.WalkAsync(catalogType);

        CatalogSeedWriter.Omitted? omitted = null;
        if (asSql)
        {
            await File.WriteAllTextAsync(outputPath, CatalogSqlWriter.Write(snapshot));
        }
        else
        {
            (var json, omitted) = CatalogSeedWriter.Write(snapshot, furnidata!);
            await File.WriteAllTextAsync(outputPath, json);
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {Path.GetFullPath(outputPath)}");
        Console.WriteLine(
            $"{snapshot.Pages.Count} page(s), {snapshot.DistinctOfferCount} offer(s) " +
            $"in {snapshot.OfferCount} listing(s), {snapshot.ProductCount} product(s).");
        if (omitted is { UnknownFurni: > 0 })
            Console.WriteLine($"{omitted.UnknownFurni} offer(s) left out: furnidata has no class " +
                              $"{string.Join(", ", omitted.UnknownClassIds.Order())}. Update seed/furnidata.xml and dump again.");
        if (omitted is { Bots: > 0 })
            Console.WriteLine($"{omitted.Bots} bot offer(s) left out: the hotel's catalog sells no bots.");

        var hidden = snapshot.Pages.Count(page => page.IsFolder);
        if (hidden > 0)
            Console.WriteLine($"{hidden} page(s) had no id to request and were written without offers.");

        // Only a page we actually asked for can have failed to answer. Counting hidden pages
        // here is what made a clean run look like it had sixteen failures.
        var silent = snapshot.Pages.Count(page => page is { Page: null, IsFolder: false });
        if (silent > 0)
            Console.WriteLine($"{silent} page(s) did not respond and were written without offers.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Dump failed: {ex.Message}");
    }
    finally
    {
        Interlocked.Exchange(ref running, 0);
    }
});

/// <summary>A path in the repository (the folder holding Habbo.slnx), found from the binary upwards so it works from
/// bin/ as from the project; as given when there is none.</summary>
static string FromRepository(string relative)
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Habbo.slnx")))
            return Path.Combine(directory.FullName, relative);
    }
    return relative;
}
