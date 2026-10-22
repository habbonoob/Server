using Habbo.Domain.Navigator;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Navigator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace Habbo.Silo.Grains.Navigator;

/// <summary>
/// Resolves a search code into result blocks, as Habbo's navigator does (measured from its own):
/// <list type="bullet">
/// <item>A tab expands to its configured blocks; some expand further, one block per room category, event category or
/// official folder.</item>
/// <item>A block lists at most its limit and offers "show more" when there were more; asking for a block's own code
/// (what "show more" sends) lists that block alone, longer.</item>
/// <item>A block with no rooms is left out, but for the favourites. Typed text narrows every block, and a tab with a
/// search block shows only that one then.</item>
/// </list>
/// Each block is shown the way the requester last chose for it.
/// </summary>
[StatelessWorker]
public sealed class NavigatorSearchGrain(
    INavigatorCatalog catalog, RoomLister lister, IDbContextFactory<AppDbContext> dbFactory, IOptions<NavigatorOptions> options)
    : Grain, INavigatorSearchGrain
{
    /// <summary>What a block's code starts with when it is one of a tab's expanded blocks.</summary>
    private const string CategoryPrefix = "category__";
    private const string EventCategoryPrefix = "eventcategory__";
    private const string OfficialPrefix = "official_";

    /// <summary>How the client localizes a room category's name.</summary>
    private const string CategoryTextKey = "${{navigator.flatcategory.global.{0}}}";

    public async Task<List<NavigatorSearchBlock>> SearchAsync(Guid requesterId, string searchCode, string filter)
    {
        Dictionary<string, int> viewModes;
        await using (var db = await dbFactory.CreateDbContextAsync())
            viewModes = await db.NavigatorViewModes.AsNoTracking().Where(v => v.UserId == requesterId)
                .ToDictionaryAsync(v => v.SearchCode, v => v.ViewMode);

        var definitions = await catalog.GetExactBlocksForAsync(searchCode);
        if (definitions.Count == 0)
            return await ShowMoreAsync(requesterId, searchCode, filter, viewModes)
                   ?? await ExpandAsync(requesterId, await catalog.GetBlocksForAsync(searchCode), filter, viewModes);

        return await ExpandAsync(requesterId, definitions, filter, viewModes);
    }

    private async Task<List<NavigatorSearchBlock>> ExpandAsync(
        Guid requesterId, IReadOnlyList<NavigatorBlockDefinition> definitions, string filter, Dictionary<string, int> viewModes)
    {
        // typed text: a tab with a search block shows that alone; without text, the search block has nothing to show
        var typed = !string.IsNullOrWhiteSpace(filter);
        if (typed && definitions.Any(d => d.Query == RoomQuery.Search))
            definitions = definitions.Where(d => d.Query == RoomQuery.Search).ToList();
        else if (!typed)
            definitions = definitions.Where(d => d.Query != RoomQuery.Search).ToList();

        var blocks = new List<NavigatorSearchBlock>();
        foreach (var definition in definitions)
        {
            foreach (var block in await BlocksOfAsync(requesterId, definition, filter, viewModes, definition.MaxResults))
            {
                if (block.Rooms.Count > 0 || definition.Query == RoomQuery.Favourites)
                    blocks.Add(block);
            }
        }

        return blocks;
    }

    /// <summary>"Show more" on a block: that block alone, as long as a search lists. Null when the code is no block's.</summary>
    private async Task<List<NavigatorSearchBlock>?> ShowMoreAsync(
        Guid requesterId, string code, string filter, Dictionary<string, int> viewModes)
    {
        var limit = options.Value.SearchResultLimit;
        var definition = code switch
        {
            _ when code.StartsWith(CategoryPrefix, StringComparison.Ordinal) =>
                new NavigatorBlockDefinition(code, string.Empty, RoomQuery.Categories, 0, 0, limit, false),
            _ when code.StartsWith(EventCategoryPrefix, StringComparison.Ordinal) =>
                new NavigatorBlockDefinition(code, string.Empty, RoomQuery.EventCategories, 0, 0, limit, false),
            _ => await catalog.GetBlockAsync(code),
        };
        if (definition is null)
            return null;

        var blocks = await BlocksOfAsync(requesterId, definition with { MaxResults = limit }, filter, viewModes, limit);
        // of an expanding block's: only the one asked for
        return blocks.Where(block => definition.Query is not (RoomQuery.Categories or RoomQuery.EventCategories or RoomQuery.OfficialFolders)
                                     || block.SearchCode == code).ToList();
    }

    /// <summary>The block a definition makes, or the blocks it expands to.</summary>
    private async Task<List<NavigatorSearchBlock>> BlocksOfAsync(
        Guid requesterId, NavigatorBlockDefinition definition, string filter, Dictionary<string, int> viewModes, int limit)
    {
        switch (definition.Query)
        {
            case RoomQuery.Categories:
            {
                var blocks = new List<NavigatorSearchBlock>();
                foreach (var category in (await catalog.GetFlatCategoriesAsync()).Where(c => c.Visible && !c.Automatic && !c.StaffOnly))
                {
                    var text = string.IsNullOrEmpty(category.GlobalCategoryKey)
                        ? category.NodeName
                        : string.Format(CategoryTextKey, category.GlobalCategoryKey);
                    blocks.Add(await BlockAsync(requesterId, definition, CategoryPrefix + category.NodeName, text, definition.ForceClosed,
                        viewModes, RoomQuery.Popular, filter, limit, categoryId: category.NodeId));
                }

                return blocks;
            }
            case RoomQuery.EventCategories:
            {
                var blocks = new List<NavigatorSearchBlock>();
                foreach (var category in (await catalog.GetEventCategoriesAsync()).Where(c => c.Visible))
                    blocks.Add(await BlockAsync(requesterId, definition, EventCategoryPrefix + category.Id, string.Empty,
                        definition.ForceClosed, viewModes, RoomQuery.Promoted, filter, limit, eventCategoryId: category.Id));
                return blocks;
            }
            case RoomQuery.OfficialFolders:
            {
                var blocks = new List<NavigatorSearchBlock>();
                foreach (var (folder, roomIds) in await OfficialFoldersAsync())
                    blocks.Add(await BlockAsync(requesterId, definition, OfficialPrefix + folder.Id, folder.PopupCaption,
                        forceClosed: !folder.Open, viewModes, RoomQuery.Popular, filter, limit, roomIds: roomIds));
                return blocks;
            }
            default:
                return [await BlockAsync(requesterId, definition, definition.Code, definition.Text, definition.ForceClosed,
                    viewModes, definition.Query, filter, limit)];
        }
    }

    /// <summary>One block: up to <paramref name="limit"/> rooms, offering more when there were more.</summary>
    private async Task<NavigatorSearchBlock> BlockAsync(
        Guid requesterId, NavigatorBlockDefinition definition, string code, string text, bool forceClosed,
        Dictionary<string, int> viewModes, RoomQuery query, string filter, int limit,
        int? categoryId = null, int? eventCategoryId = null, IReadOnlyList<int>? roomIds = null)
    {
        var rooms = await lister.ListAsync(requesterId, query, filter, limit + 1, categoryId, eventCategoryId, roomIds);
        var more = rooms.Count > limit;
        return new NavigatorSearchBlock(
            code,
            text,
            more ? 1 : 0,
            forceClosed,
            viewModes.GetValueOrDefault(code, definition.ViewMode),
            more ? rooms.Take(limit).ToList() : rooms);
    }

    /// <summary>The official rooms' top folders, each with its rooms: those it names, then those carrying its tags.</summary>
    private async Task<List<(OfficialRoomEntry Folder, List<int> RoomIds)>> OfficialFoldersAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var entries = await db.OfficialRoomEntries.AsNoTracking().Where(e => !e.IsAd).OrderBy(e => e.SortOrder).ToListAsync();
        var tags = entries.Where(e => e.Type == OfficialRoomEntryType.Tag && e.Tag is not null).Select(e => e.Tag!).Distinct().ToList();
        var tagged = await db.Rooms.AsNoTracking().Where(r => r.Tags.Any(t => tags.Contains(t)))
            .OrderBy(r => r.Id).Select(r => new { r.Id, r.Tags }).ToListAsync();

        return entries
            .Where(e => e.FolderId is null && e.Type == OfficialRoomEntryType.Folder)
            .Select(folder =>
            {
                var children = entries.Where(e => e.FolderId == folder.Id).ToList();
                var roomIds = children.Where(e => e.Type == OfficialRoomEntryType.Room && e.RoomId is not null).Select(e => e.RoomId!.Value)
                    .Concat(children.Where(e => e.Type == OfficialRoomEntryType.Tag)
                        .SelectMany(e => tagged.Where(r => r.Tags.Contains(e.Tag!)).Select(r => r.Id)))
                    .Distinct()
                    .ToList();
                return (folder, roomIds);
            })
            .ToList();
    }
}
