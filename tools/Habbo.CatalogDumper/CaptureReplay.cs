using System.Text.Json;
using Xabbo;
using Xabbo.Core;
using Xabbo.Messages;

namespace Habbo.CatalogDumper;

/// <summary>A catalog read back from a capture of the web client's traffic (tools/Habbo.Relay's
/// habbo-packetlogs/captures/*.jsonl) instead of walked live through G-Earth: the index of the catalog type and the
/// page answers, which the client asked for itself (habbo-pixi's packet taps), one at a time as the walker would.
/// The last answer for a page wins; a page the capture has no answer for is kept with none, as one that timed out.</summary>
public static class CaptureReplay
{
    private sealed record CapturedPacket(string Dir, int Id, string Name, string? Hex);

    public static CatalogSnapshot Read(string path, string catalogType)
    {
        Catalog? index = null;
        var answers = new Dictionary<int, CatalogPage>();
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length == 0)
                continue;
            var packet = JsonSerializer.Deserialize<CapturedPacket>(line, JsonOptions);
            if (packet is not { Dir: "in", Hex: { Length: > 0 } hex })
                continue;

            switch (packet.Name)
            {
                case "CatalogIndexMessageEvent":
                    var catalog = Parse(packet.Id, hex).Read<Catalog>();
                    if (catalog.Type == catalogType)
                        index = catalog;
                    break;
                case "CatalogPageMessageEvent":
                    var page = Parse(packet.Id, hex).Read<CatalogPage>();
                    if (page.CatalogType == catalogType)
                        answers[page.Id] = page;
                    break;
            }
        }

        if (index is null)
            throw new InvalidOperationException($"{path} has no {catalogType} catalog index.");

        var folderIds = new HashSet<int>();
        var pages = CatalogWalker.Flatten(index.RootNode, parentId: null, folderIds)
            .Select(node => new CapturedPage(node.Node, node.IsFolder ? null : answers.GetValueOrDefault(node.Id))
            {
                Id = node.Id, ParentId = node.ParentId, OrderIndex = node.Order, IsFolder = node.IsFolder,
            })
            .ToList();
        return new CatalogSnapshot(catalogType, pages);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static Packet Parse(int id, string hex) =>
        new(new Header(Direction.In, (short)id), ClientType.Flash, new PacketBuffer(Convert.FromHexString(hex)));
}
