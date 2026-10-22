namespace Habbo.Domain.Navigator;

/// <summary>One result block a search code expands into. A request code may produce several
/// blocks, which is how a tab shows more than one list.</summary>
public sealed class NavigatorBlock
{
    public int Id { get; set; }

    /// <summary>The search code the client asked for.</summary>
    public required string RequestCode { get; set; }

    /// <summary>The code reported back on the block itself.</summary>
    public required string BlockCode { get; set; }

    public required string Text { get; set; }

    /// <summary>Which rooms this block lists, as the name of a RoomQuery value. Stored as text so
    /// adding a query is a seed change plus a case, not a schema change.</summary>
    public required string Query { get; set; }

    /// <summary>Client action code: 0 none, 1 any, 2 owner only.</summary>
    public int ActionAllowed { get; set; }

    /// <summary>Client view mode: 0 list, 1 tiles, 2 thumbnails.</summary>
    public int ViewMode { get; set; }

    public int MaxResults { get; set; }

    public bool ForceClosed { get; set; }

    public int SortOrder { get; set; }
}
