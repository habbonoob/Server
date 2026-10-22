using Habbo.Domain.Users;
using Habbo.Infrastructure;
using Habbo.Silo.Abstractions.Navigator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Navigator;

/// <summary>The navigator layout as stored in the database: which tabs exist, what each
/// search code lists, and the category rows. Changing the navigator is a data change.</summary>
public sealed class NavigatorCatalog(
    IDbContextFactory<AppDbContext> dbFactory,
    IOptions<NavigatorOptions> options) : INavigatorCatalog
{
    public async Task<IReadOnlyList<NavigatorContext>> GetContextsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tabs = await db.NavigatorTabs
            .AsNoTracking()
            .Include(tab => tab.QuickLinks)
            .OrderBy(tab => tab.SortOrder)
            .ToListAsync(ct);

        return tabs
            .Select(tab => new NavigatorContext(
                tab.SearchCode,
                tab.QuickLinks
                    .OrderBy(link => link.SortOrder)
                    .Select(link => new NavigatorSavedSearch(link.Id, link.SearchCode, link.Filter, link.Localization))
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<EventCategory>> GetEventCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.RoomEventCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .Select(category => new EventCategory(category.Id, category.Name, category.Visible))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NavigatorBlockDefinition>> GetBlocksForAsync(
        string searchCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var blocks = await LoadBlocksAsync(db, searchCode, ct);
        if (blocks.Count == 0 && searchCode != options.Value.FallbackRequestCode)
            blocks = await LoadBlocksAsync(db, options.Value.FallbackRequestCode, ct);

        return blocks;
    }

    public async Task<IReadOnlyList<NavigatorBlockDefinition>> GetExactBlocksForAsync(string requestCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await LoadBlocksAsync(db, requestCode, ct);
    }

    public async Task<NavigatorBlockDefinition?> GetBlockAsync(string blockCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NavigatorBlocks.AsNoTracking()
            .Where(block => block.BlockCode == blockCode)
            .OrderBy(block => block.SortOrder)
            .FirstOrDefaultAsync(ct);
        return row is null ? null : ToDefinition(row);
    }

    private static async Task<List<NavigatorBlockDefinition>> LoadBlocksAsync(
        AppDbContext db, string requestCode, CancellationToken ct)
    {
        var rows = await db.NavigatorBlocks
            .AsNoTracking()
            .Where(block => block.RequestCode == requestCode)
            .OrderBy(block => block.SortOrder)
            .ToListAsync(ct);

        return rows.Select(ToDefinition).ToList();
    }

    private static NavigatorBlockDefinition ToDefinition(Habbo.Domain.Navigator.NavigatorBlock block) =>
        new(
            block.BlockCode,
            block.Text,
            Enum.TryParse<RoomQuery>(block.Query, ignoreCase: true, out var query) ? query : RoomQuery.Popular,
            block.ActionAllowed,
            block.ViewMode,
            block.MaxResults,
            block.ForceClosed);

    public async Task<IReadOnlyList<FlatCategory>> GetFlatCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.NavigatorCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ToListAsync(ct);

        return rows
            .Select(category => new FlatCategory(
                category.Id,
                category.NodeName,
                category.Visible,
                category.Automatic,
                category.AutomaticCategoryKey,
                category.GlobalCategoryKey,
                category.StaffOnly))
            .ToList();
    }
}

/// <summary>Navigator behaviour that isn't per-row data. Bound from the "Navigator" section.</summary>
public sealed class NavigatorOptions
{
    /// <summary>The request code used when the client asks for one we have no blocks for.</summary>
    public string FallbackRequestCode { get; set; } = "hotel_view";

    /// <summary>How many rooms a user may star.</summary>
    public int FavouriteLimit { get; set; } = 30;

    /// <summary>How many entries the tag cloud returns.</summary>
    public int PopularTagCount { get; set; } = 20;

    /// <summary>How many rooms one of the old navigator's searches lists.</summary>
    public int SearchResultLimit { get; set; } = 50;

    /// <summary>How many of a competition's rooms one page lists.</summary>
    public int CompetitionPageSize { get; set; } = 20;

    /// <summary>How many rooms each promoted category of the official view shows.</summary>
    public int PromotedRoomsPerCategory { get; set; } = 10;

    /// <summary>How many searches a player may save to the new navigator.</summary>
    public int SavedSearchLimit { get; set; } = 25;

    /// <summary>The ways a result block can be shown: a list, or thumbnails.</summary>
    public List<int> ViewModes { get; set; } = [0, 1];

    public NavigatorWindowDefaults DefaultWindow { get; set; } = new();

    /// <summary>How long a room ad runs, and the layout of the catalog page its offers are on.</summary>
    public int RoomAdDurationMinutes { get; set; } = 120;

    public string RoomAdPageLayout { get; set; } = string.Empty;

    /// <summary>What a room link from outside the client puts before the room's id.</summary>
    public string GlobalRoomIdPrefix { get; set; } = "r-hh-";
}
