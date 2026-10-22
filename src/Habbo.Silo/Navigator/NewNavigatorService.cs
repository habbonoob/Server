using Habbo.Domain.Navigator;
using Habbo.Infrastructure;
using Habbo.Protocol.Messages.Outgoing.NewNavigator;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Navigator;

/// <summary>What the new navigator remembers for each player.</summary>
public sealed class NewNavigatorService(
    IDbContextFactory<AppDbContext> dbFactory,
    PlayerMessenger messenger,
    IOptions<NavigatorOptions> options) : INewNavigatorService
{
    /// <summary>A saved search has no text of its own; the client names it from its code and filter.</summary>
    private const string NoLocalization = "";

    public async Task SendPlayerStateAsync(PlayerData player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var lifted = await db.LiftedRooms.AsNoTracking().OrderBy(r => r.SortOrder)
            .Select(r => new LiftedRoomData(r.RoomId, r.AreaId, r.Image, r.Caption)).ToListAsync();
        await messenger.SendAsync(player.Id, new NavigatorLiftedRoomsComposer(lifted));

        var collapsed = await db.NavigatorCollapsedCategories.AsNoTracking()
            .Where(c => c.UserId == player.Id).Select(c => c.Category).ToListAsync();
        await messenger.SendAsync(player.Id, new NavigatorCollapsedCategoriesMessageComposer(collapsed));

        await SendSavedSearchesAsync(db, player);

        var window = await db.NavigatorWindowPreferences.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == player.Id);
        var defaults = options.Value.DefaultWindow;
        await messenger.SendAsync(player.Id, window is null
            ? new NewNavigatorPreferencesComposer(defaults.X, defaults.Y, defaults.Width, defaults.Height, defaults.LeftPaneHidden, defaults.ResultsMode)
            : new NewNavigatorPreferencesComposer(window.X, window.Y, window.Width, window.Height, window.LeftPaneHidden, window.ResultsMode));
    }

    private async Task SendSavedSearchesAsync(AppDbContext db, PlayerData player)
    {
        var searches = await db.NavigatorSavedSearches.AsNoTracking()
            .Where(s => s.UserId == player.Id).OrderBy(s => s.Id)
            .Select(s => new NavigatorQuickLinkData(s.Id, s.SearchCode, s.Filter, NoLocalization)).ToListAsync();
        await messenger.SendAsync(player.Id, new NavigatorSavedSearchesComposer(searches));
    }

    public async Task AddSavedSearchAsync(PlayerData player, string searchCode, string filter)
    {
        if (string.IsNullOrWhiteSpace(searchCode) || searchCode.Length > SavedNavigatorSearch.SearchCodeMaxLength
            || filter.Length > SavedNavigatorSearch.FilterMaxLength)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var saved = db.NavigatorSavedSearches.Where(s => s.UserId == player.Id);
        if (await saved.CountAsync() >= options.Value.SavedSearchLimit
            || await saved.AnyAsync(s => s.SearchCode == searchCode && s.Filter == filter))
            return;

        db.NavigatorSavedSearches.Add(new SavedNavigatorSearch { UserId = player.Id, SearchCode = searchCode, Filter = filter });
        await db.SaveChangesAsync();
        await SendSavedSearchesAsync(db, player);
    }

    public async Task DeleteSavedSearchAsync(PlayerData player, int searchId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.NavigatorSavedSearches.Where(s => s.Id == searchId && s.UserId == player.Id).ExecuteDeleteAsync() > 0)
            await SendSavedSearchesAsync(db, player);
    }

    /// <summary>The most collapsed categories, and the most view modes, a player keeps. Their names are the client's own,
    /// so a player is limited to about as many as the navigator has, not to however many a client makes up.</summary>
    private const int MaxStoredPreferences = 100;

    public async Task SetCategoryCollapsedAsync(PlayerData player, string category, bool collapsed)
    {
        if (string.IsNullOrEmpty(category) || category.Length > NavigatorCollapsedCategory.CategoryMaxLength)
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.NavigatorCollapsedCategories.FirstOrDefaultAsync(c => c.UserId == player.Id && c.Category == category);
        if (collapsed && existing is null && await db.NavigatorCollapsedCategories.CountAsync(c => c.UserId == player.Id) >= MaxStoredPreferences)
            return;
        if (collapsed && existing is null)
            db.NavigatorCollapsedCategories.Add(new NavigatorCollapsedCategory { UserId = player.Id, Category = category });
        else if (!collapsed && existing is not null)
            db.NavigatorCollapsedCategories.Remove(existing);
        await db.SaveChangesAsync();
    }

    public async Task SetViewModeAsync(PlayerData player, string searchCode, int viewMode)
    {
        if (string.IsNullOrEmpty(searchCode) || searchCode.Length > SavedNavigatorSearch.SearchCodeMaxLength
            || !options.Value.ViewModes.Contains(viewMode))
            return;

        await using var db = await dbFactory.CreateDbContextAsync();
        var existing = await db.NavigatorViewModes.FirstOrDefaultAsync(v => v.UserId == player.Id && v.SearchCode == searchCode);
        if (existing is null && await db.NavigatorViewModes.CountAsync(v => v.UserId == player.Id) >= MaxStoredPreferences)
            return;
        if (existing is null)
            db.NavigatorViewModes.Add(new NavigatorViewMode { UserId = player.Id, SearchCode = searchCode, ViewMode = viewMode });
        else
            existing.ViewMode = viewMode;
        await db.SaveChangesAsync();
    }

    public async Task SetWindowPreferencesAsync(PlayerData player, int x, int y, int width, int height, bool leftPaneHidden, int resultsMode)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var window = await db.NavigatorWindowPreferences.FirstOrDefaultAsync(w => w.UserId == player.Id);
        if (window is null)
            db.NavigatorWindowPreferences.Add(window = new NavigatorWindowPreferences { UserId = player.Id });

        window.X = x;
        window.Y = y;
        window.Width = Math.Max(0, width);
        window.Height = Math.Max(0, height);
        window.LeftPaneHidden = leftPaneHidden;
        window.ResultsMode = options.Value.ViewModes.Contains(resultsMode) ? resultsMode : window.ResultsMode;
        await db.SaveChangesAsync();
    }
}
