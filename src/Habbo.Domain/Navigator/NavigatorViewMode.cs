namespace Habbo.Domain.Navigator;

/// <summary>How a player likes one search code's results shown: a list or thumbnails.</summary>
public sealed class NavigatorViewMode
{
    public Guid UserId { get; set; }

    public required string SearchCode { get; set; }

    public int ViewMode { get; set; }
}
