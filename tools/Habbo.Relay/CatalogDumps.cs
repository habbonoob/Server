using System.Text;
using System.Text.Json;
using Habbo.CatalogDumper;
using Microsoft.Extensions.Options;

namespace Habbo.Relay;

/// <summary>A catalog the web client's catalog dumper (habbo-pixi's catalog-dumper) walked: the catalog type and the
/// index and page packets Habbo answered with, as the client received them.</summary>
/// <param name="CatalogType">The catalog that was walked, e.g. "NORMAL" or "BUILDERS_CLUB".</param>
/// <param name="Packets">The CatalogIndexMessageEvent and CatalogPageMessageEvent packets, in the order they came.</param>
public sealed record CatalogDump(string CatalogType, List<CapturedPacket> Packets);

/// <summary>What became of a catalog dump: where it was written and what it held, as the G-Earth dumper reports it.</summary>
public sealed record CatalogDumpResult(
    string File,
    string Capture,
    int Pages,
    int Offers,
    int Listings,
    int Products,
    int Folders,
    int Silent,
    int UnknownFurni,
    IReadOnlyCollection<int> UnknownClassIds,
    int Bots);

/// <summary>Writes the catalogs the web client's catalog dumper walked as tools/Habbo.CatalogDumper writes its own:
/// the packets are kept as a capture (habbo-packetlogs/captures/*_catalog-*.jsonl), which the dumper's capture replay
/// reads back and its seed writer turns into the seed catalog. The same capture gives the same seed through
/// <c>Habbo.CatalogDumper --capture</c>, and SQL from there with <c>CATALOG_OUTPUT=*.sql</c>.</summary>
public sealed class CatalogDumps(IOptions<RelayOptions> options, ILogger<CatalogDumps> logger)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _captures = CaptureLog.Resolve(options.Value.CaptureDirectory);
    private readonly string _seed = CaptureLog.Resolve(options.Value.CatalogSeedDirectory);
    private readonly string _furnidata = CaptureLog.Resolve(options.Value.CatalogFurnidata);

    /// <summary>One dump at a time: two at once would write the same seed file.</summary>
    private readonly SemaphoreSlim _writing = new(1, 1);

    /// <summary>The seed file a catalog type is written to, as the hotel's seeder reads them: the shop's catalog.json,
    /// the Builders Club warehouse's catalog_builders_club.json.</summary>
    public string SeedPath(string catalogType) => Path.Combine(_seed,
        catalogType == "NORMAL" ? "catalog.json" : $"catalog_{catalogType.ToLowerInvariant()}.json");

    /// <summary>Keeps the packets as a capture and writes the seed catalog from it.</summary>
    /// <exception cref="InvalidOperationException">The packets hold no index of the catalog type.</exception>
    public async Task<CatalogDumpResult> SaveAsync(CatalogDump dump, CancellationToken ct)
    {
        await _writing.WaitAsync(ct);
        try
        {
            var capture = await WriteCaptureAsync(dump, ct);
            CatalogSnapshot snapshot;
            try
            {
                snapshot = CaptureReplay.Read(capture, dump.CatalogType);
            }
            catch (InvalidOperationException)
            {
                // no index of that catalog: nothing worth keeping
                File.Delete(capture);
                throw;
            }
            var furnidata = CatalogSeedWriter.ReadFurnidata(_furnidata);
            var (json, omitted) = CatalogSeedWriter.Write(snapshot, furnidata);

            var seed = SeedPath(dump.CatalogType);
            Directory.CreateDirectory(Path.GetDirectoryName(seed)!);
            await File.WriteAllTextAsync(seed, json, ct);
            logger.LogInformation("Catalog {Type} dumped: {Pages} pages, {Offers} offers to {Path}.", dump.CatalogType,
                snapshot.Pages.Count, snapshot.DistinctOfferCount, seed);

            return new CatalogDumpResult(
                File: seed,
                Capture: capture,
                Pages: snapshot.Pages.Count,
                Offers: snapshot.DistinctOfferCount,
                Listings: snapshot.OfferCount,
                Products: snapshot.ProductCount,
                Folders: snapshot.Pages.Count(page => page.IsFolder),
                Silent: snapshot.Pages.Count(page => page is { Page: null, IsFolder: false }),
                UnknownFurni: omitted.UnknownFurni,
                UnknownClassIds: omitted.UnknownClassIds.Order().ToList(),
                Bots: omitted.Bots);
        }
        finally
        {
            _writing.Release();
        }
    }

    /// <summary>The packets as a capture of their own, in the relay's .jsonl capture format.</summary>
    private async Task<string> WriteCaptureAsync(CatalogDump dump, CancellationToken ct)
    {
        Directory.CreateDirectory(_captures);
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var path = Path.Combine(_captures, $"{stamp}_catalog-{dump.CatalogType.ToLowerInvariant()}.jsonl");
        for (var n = 2; File.Exists(path); n++)
            path = Path.Combine(_captures, $"{stamp}_catalog-{dump.CatalogType.ToLowerInvariant()}-{n}.jsonl");

        var lines = new StringBuilder();
        var seq = 0;
        foreach (var packet in dump.Packets)
        {
            lines.AppendLine(JsonSerializer.Serialize(new
            {
                seq = ++seq,
                time = DateTimeOffset.FromUnixTimeMilliseconds(packet.Time),
                dir = packet.Dir,
                id = packet.Id,
                name = packet.Name,
                length = packet.Hex.Length / 2,
                hex = packet.Hex,
            }, Json));
        }
        await File.WriteAllTextAsync(path, lines.ToString(), new UTF8Encoding(false), ct);
        return path;
    }
}
