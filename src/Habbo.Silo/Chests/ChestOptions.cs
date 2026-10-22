namespace Habbo.Silo.Chests;

/// <summary>Chests (config section "Chests"); how much they hold and cost is the client's own variables.</summary>
public sealed class ChestOptions
{
    /// <summary>Items per contents fragment; a full chest arrives in several.</summary>
    public int ContentsFragmentSize { get; set; }
}
