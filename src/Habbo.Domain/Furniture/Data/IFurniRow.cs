namespace Habbo.Domain.Furniture.Data;

/// <summary>A row in one of the furni_* tables: every one hangs off a single item.</summary>
public interface IFurniRow
{
    int ItemId { get; set; }
    UserItem? Item { get; set; }
}
