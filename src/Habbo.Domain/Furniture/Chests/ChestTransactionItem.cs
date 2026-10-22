namespace Habbo.Domain.Furniture.Chests;

/// <summary>How many items of one kind a transaction put into chests, or took out.</summary>
public sealed class ChestTransactionItem
{
    public const int PosterIdMaxLength = 32;

    public long TransactionId { get; set; }

    public bool Deposited { get; set; }

    public bool IsWallItem { get; set; }

    /// <summary>The kind's class id, as furnidata numbers it.</summary>
    public int TypeId { get; set; }

    /// <summary>Which poster, for the one wall item that comes in many.</summary>
    public string PosterId { get; set; } = string.Empty;

    public int Amount { get; set; }
}
