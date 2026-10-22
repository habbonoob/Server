namespace Habbo.Domain.Navigator;

/// <summary>A top-level navigator tab ("official_view", "hotel_view", "myworld_view").
/// The client asks for one by search code and gets its quick links back.</summary>
public sealed class NavigatorTab
{
    public required string SearchCode { get; set; }

    /// <summary>Order the tabs are sent in; the client keeps this order.</summary>
    public int SortOrder { get; set; }

    public List<NavigatorQuickLink> QuickLinks { get; set; } = [];
}
