namespace Habbo.Domain.Navigator;

/// <summary>A room category, as UserFlatCats lists it.</summary>
public sealed class NavigatorCategory
{
    public int Id { get; set; }

    public required string NodeName { get; set; }

    public bool Visible { get; set; } = true;

    public bool Automatic { get; set; }

    public string AutomaticCategoryKey { get; set; } = string.Empty;

    public string GlobalCategoryKey { get; set; } = string.Empty;

    public bool StaffOnly { get; set; }

    /// <summary>False for UserFlatCats, true for UserEventCats.</summary>
    public bool IsEventCategory { get; set; }

    public int SortOrder { get; set; }
}
