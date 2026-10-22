using Xabbo;
using Xabbo.Interceptor;
using Xabbo.Core;
using Xabbo.Core.Messages.Outgoing;
using Xabbo.Messages;
using Xabbo.Messages.Flash;

namespace Habbo.CatalogDumper;

/// <summary>Walks the live catalog: asks for the index, then for each page in the tree.
///
/// The requests go out one at a time with a delay between them. The catalog is a few hundred
/// pages and Habbo rate-limits; hammering it would get the account throttled and produce a
/// half-empty dump that looks complete.</summary>
public sealed class CatalogWalker(IInterceptor interceptor, int requestDelayMs = 600)
{
    /// <summary>How long to wait for a single response before giving up on that page.</summary>
    private const int ResponseTimeoutMs = 15_000;

    public async Task<CatalogSnapshot> WalkAsync(
        string catalogType = "NORMAL", CancellationToken ct = default)
    {
        var index = await RequestIndexAsync(catalogType, ct);
        Console.WriteLine($"Catalog index received (type: {index.Type}).");

        // The root itself is the tree's anchor and is never rendered, so only its descendants
        // become pages.
        var folderIds = new HashSet<int>();
        var nodes = Flatten(index.RootNode, parentId: null, folderIds).ToList();
        var requestable = nodes.Count(node => !node.IsFolder);
        Console.WriteLine($"Walking {requestable} page(s) ({nodes.Count - requestable} folder(s) skipped)...");

        var pages = new List<CapturedPage>(nodes.Count);
        var requested = 0;

        foreach (var (node, id, parentId, order, isFolder) in nodes)
        {
            ct.ThrowIfCancellationRequested();

            // Habbo hid this page's id, so there is nothing to ask for. Requesting -1 costs a
            // full timeout and answers nothing, which is what made a dump take minutes longer
            // than it needed to.
            if (isFolder)
            {
                Console.WriteLine($"[{"folder",9}] {node.Name}");
                pages.Add(new CapturedPage(node, Page: null)
                {
                    Id = id, ParentId = parentId, OrderIndex = order, IsFolder = true,
                });
                continue;
            }

            if (requested++ > 0)
                await Task.Delay(requestDelayMs, ct);

            Console.Write($"[{requested,4}/{requestable,-4}] {node.Name} ... ");

            var page = await RequestPageAsync(id, catalogType, ct);
            Console.WriteLine(page is null ? "no response" : $"{page.Offers.Count} offer(s)");

            pages.Add(new CapturedPage(node, page) { Id = id, ParentId = parentId, OrderIndex = order });
        }

        return new CatalogSnapshot(catalogType, pages);
    }

    private async Task<Catalog> RequestIndexAsync(string catalogType, CancellationToken ct)
    {
        // The index has no request message type in Xabbo.Core, so the packet goes out raw and
        // the response is captured by identifier.
        var capture = interceptor.ReceiveAsync([In.CatalogIndex], ResponseTimeoutMs, cancellationToken: ct);
        interceptor.Send(Out.GetCatalogIndex, catalogType);

        var packet = await capture;
        return packet.Read<Catalog>();
    }

    /// <summary>Null when the page did not answer in time — a page we cannot read is skipped
    /// rather than silently recorded as empty.
    ///
    /// The capture only accepts a response carrying the page id we asked for. Without that,
    /// a page that answered just after its own timeout would be picked up by the next page's
    /// capture, and its offers would be filed under the wrong page — a corrupt dump that
    /// looks perfectly well formed.</summary>
    private async Task<CatalogPage?> RequestPageAsync(int pageId, string catalogType, CancellationToken ct)
    {
        try
        {
            var capture = interceptor.ReceiveAsync([In.CatalogPage], ResponseTimeoutMs,
                shouldCapture: packet => PeekPageId(packet) == pageId,
                cancellationToken: ct);
            interceptor.Send(new GetCatalogPageMsg(pageId, -1, catalogType));

            var packet = await capture;
            return packet.Read<CatalogPage>();
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    /// <summary>The page id without consuming the packet: it is the first field, and reading
    /// from a local position leaves the packet untouched for whoever captures it.</summary>
    private static int PeekPageId(IPacket packet)
    {
        var position = 0;
        return packet.ReaderAt(ref position).ReadInt();
    }

    /// <summary>Depth-first, parents before children, so a page's parent always exists by the
    /// time the insert for it runs. A page whose id Habbo hid is given one derived from its
    /// name, and its children are given that same id as their parent — otherwise every hidden
    /// page would be stored as -1 and the whole tree would collapse onto one row.</summary>
    internal static IEnumerable<(ICatalogPageNode Node, int Id, int? ParentId, int Order, bool IsFolder)> Flatten(
        ICatalogPageNode root, int? parentId, HashSet<int> folderIds)
    {
        var order = 0;

        foreach (var child in root.Children)
        {
            var isFolder = child.Id <= HiddenPageIdentity.HiddenPageId;
            var id = isFolder ? HiddenPageIdentity.Derive(child.Name, folderIds) : child.Id;

            yield return (child, id, parentId, order++, isFolder);

            foreach (var descendant in Flatten(child, id, folderIds))
                yield return descendant;
        }
    }
}
