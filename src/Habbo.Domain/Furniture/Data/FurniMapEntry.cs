namespace Habbo.Domain.Furniture.Data;

/// <summary>One key/value pair of a map-shaped item (type 1). Keyed by the pair, because the
/// client reads them into a map and a repeated key would simply overwrite itself.</summary>
public sealed class FurniMapEntry : IFurniRow
{
    public int ItemId { get; set; }
    public UserItem? Item { get; set; }

    public required string Key { get; set; }
    public required string Value { get; set; }
}
