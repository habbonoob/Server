namespace Habbo.Domain.Furniture.Chests;

/// <summary>A change to what chests hold: who made it, how, and how much went in and out. Its items say which kinds.</summary>
public sealed class ChestTransaction
{
    /// <summary>The kinds of transaction, as the client names them ("wired_transactions.type.N").</summary>
    public const int Manual = 0;
    public const int Wired = 1;
    public const int ContractPayment = 2;
    public const int ContractReward = 3;
    public const int ContractTrade = 4;
    public const int AutoWithdraw = 5;

    public const int InfoMaxLength = 128;

    public long Id { get; set; }

    public int RoomId { get; set; }

    public int Type { get; set; }

    /// <summary>For a wired transaction the effect and amount; for a contract its id and multiplier.</summary>
    public string Info { get; set; } = string.Empty;

    /// <summary>Who it was for; null when wired moved things on its own.</summary>
    public Guid? UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<int> ChestIds { get; set; } = [];

    public int DepositedFurni { get; set; }

    public int WithdrawnFurni { get; set; }

    public int DepositedCoins { get; set; }

    public int WithdrawnCoins { get; set; }

    public List<ChestTransactionItem> Items { get; set; } = [];
}
