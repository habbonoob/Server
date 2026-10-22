namespace Habbo.Silo.Users;

/// <summary>The hotel's rules for accounts (config section "Account").</summary>
public sealed class AccountOptions
{
    /// <summary>A name's length, as the client's name change dialog tells the player.</summary>
    public int NameMinLength { get; set; } = 3;

    public int NameMaxLength { get; set; } = 15;

    /// <summary>What a name may hold besides letters and digits.</summary>
    public string NameExtraCharacters { get; set; } = "-=?!@:.,_";

    /// <summary>How many other names to offer when one is taken.</summary>
    public int NameSuggestionCount { get; set; } = 3;

    /// <summary>The motto box's own limit (user_view's motto_text max_chars).</summary>
    public int MottoMaxLength { get; set; } = 38;

    /// <summary>How often a day a player may buy their respects back.</summary>
    public int RespectReplenishesPerDay { get; set; } = 1;

    /// <summary>What a respect refill costs when the client variables do not say; the client's
    /// own fallback for respect.replenish_cost_duckets.</summary>
    public int RespectReplenishCostFallback { get; set; } = 50;

    /// <summary>Badges per BadgesEvent fragment.</summary>
    public int BadgeFragmentSize { get; set; } = 500;

    /// <summary>The messages of the day, shown once a login.</summary>
    public List<string> MessagesOfTheDay { get; set; } = [];

    /// <summary>How many users a player may mute; muting one more drops the oldest.</summary>
    public int IgnoreListLimit { get; set; } = 100;

    /// <summary>The email rules the client's error texts state.</summary>
    public int EmailMaxLength { get; set; } = 48;

    public int EmailMaxChanges { get; set; } = 10;

    /// <summary>How many star gems a player may give a day.</summary>
    public int StarGemsPerDay { get; set; } = 1;

    /// <summary>How far back a player must have logged in for their look to be a hot look.</summary>
    public int HotLooksRecentDays { get; set; } = 7;

    /// <summary>The most hot looks one request gets.</summary>
    public int HotLooksMaxAmount { get; set; } = 30;
}
