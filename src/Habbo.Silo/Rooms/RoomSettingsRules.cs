using Habbo.Silo.Abstractions.Permissions;
using Habbo.Silo.Abstractions.Navigator;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Chat;
using Habbo.Silo.Permissions;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Rooms;

/// <summary>What a room's settings may be, whether set when the room is made or changed after: a name and
/// description the filter passes, cut to length, a visitor limit within the hotel's, a category the player may
/// pick, and the settings window's choices within what it offers.</summary>
public sealed class RoomSettingsRules(
    IOptions<RoomOptions> options,
    WordFilter wordFilter,
    INavigatorCatalog navigatorCatalog,
    PermissionService permissions)
{
    // the settings window's choices, as it numbers them
    public const int TradeModeMax = 2;
    public const int ThicknessMin = -2;
    public const int ThicknessMax = 1;
    public const int WhoCanMuteMax = 1;
    public const int WhoCanKickMax = 2;
    public const int WhoCanBanMax = 1;
    public const int ChatModeMax = 1;
    public const int ChatBubbleWidthMax = 2;
    public const int ChatScrollSpeedMax = 2;
    public const int ChatHearRangeMax = 99;
    public const int ChatFloodSensitivityMax = 2;

    public Task<bool> IsCleanAsync(string text) => wordFilter.IsCleanAsync(text);

    public string Name(string name) => Clip(name.Trim(), options.Value.NameMaxLength);

    public string Description(string description) => Clip(description.Trim(), options.Value.DescriptionMaxLength);

    public int MaxVisitors(int requested) => Math.Clamp(requested, 1, options.Value.MaxVisitorsLimit);

    /// <summary>A room may be put in a visible category that is not filled automatically; a staff-only one needs staff.</summary>
    public async Task<bool> MayPickCategoryAsync(PlayerData player, int categoryId) =>
        (await navigatorCatalog.GetFlatCategoriesAsync()).Any(c => c.NodeId == categoryId && c.Visible && !c.Automatic
            && (!c.StaffOnly || permissions.Has(player, PermissionNames.NavigatorStaffCategories)));

    /// <summary>A choice the window offers, or what it was when the one asked for is not.</summary>
    public static int Choice(int requested, int min, int max, int current) => requested >= min && requested <= max ? requested : current;

    private static string Clip(string text, int length) => text.Length > length ? text[..length] : text;
}
