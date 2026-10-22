namespace Habbo.Domain.Users;

/// <summary>One of the hotel's activity-point currencies — duckets, diamonds, the seasonal ones.
/// The type is the client's own number: it picks the icon and the purse slot from it. Habbo lists
/// every currency it runs at login, held or not, so the hotel's list lives here rather than being
/// inferred from what players happen to hold.</summary>
public sealed class CurrencyType
{
    public int Type { get; set; }

    /// <summary>A name for people reading the table; the client never sees it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What a new account starts with.</summary>
    public int InitialAmount { get; set; }
}
