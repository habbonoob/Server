using Habbo.Silo.Abstractions.Players;

namespace Habbo.Silo.Abstractions.Navigator;

/// <summary>What the new navigator remembers for each player: saved searches, folded blocks, how each
/// search's results are shown, and where its window sits. Every call answers the player who made it.</summary>
public interface INewNavigatorService
{
    /// <summary>What the navigator opens with: the lifted rooms and the player's own arrangement.</summary>
    Task SendPlayerStateAsync(PlayerData player);

    Task AddSavedSearchAsync(PlayerData player, string searchCode, string filter);

    Task DeleteSavedSearchAsync(PlayerData player, int searchId);

    Task SetCategoryCollapsedAsync(PlayerData player, string category, bool collapsed);

    Task SetViewModeAsync(PlayerData player, string searchCode, int viewMode);

    Task SetWindowPreferencesAsync(PlayerData player, int x, int y, int width, int height, bool leftPaneHidden, int resultsMode);
}
