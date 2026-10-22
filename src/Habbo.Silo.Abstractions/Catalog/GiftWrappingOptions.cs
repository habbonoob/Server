namespace Habbo.Silo.Abstractions.Catalog;

/// <summary>How gifts may be wrapped (config "Catalog:GiftWrapping"): the paid wraps and the free ones
/// by furni class name, the boxes and ribbons offered, and what a paid wrap costs in credits.</summary>
public sealed class GiftWrappingOptions
{
    public bool Enabled { get; set; }

    public int Price { get; set; }

    public List<string> WrapClassNames { get; set; } = [];

    public List<string> DefaultClassNames { get; set; } = [];

    public List<int> BoxTypes { get; set; } = [];

    public List<int> RibbonTypes { get; set; } = [];

    /// <summary>The longest message a gift carries.</summary>
    public int MessageMaxLength { get; set; } = 140;
}
