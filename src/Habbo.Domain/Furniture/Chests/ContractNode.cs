namespace Habbo.Domain.Furniture.Chests;

/// <summary>One element of a contract rule: so many credits, or so many items of one kind.</summary>
public sealed class ContractNode
{
    public const byte Credits = 0;
    public const byte Furni = 1;

    public byte Type { get; set; }

    public int Amount { get; set; }

    public bool IsWallItem { get; set; }

    /// <summary>The kind's class id, for a furni element.</summary>
    public int TypeId { get; set; }

    /// <summary>Which poster, for the one wall item that comes in many.</summary>
    public string PosterId { get; set; } = string.Empty;
}
