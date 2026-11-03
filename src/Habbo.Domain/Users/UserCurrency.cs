namespace Habbo.Domain.Users;

/// <summary>What a player holds of one activity-point currency, or of emeralds.</summary>
public sealed class UserCurrency
{
    /// <summary>The type emeralds are held (and their transactions recorded) under. They are not an activity point: the
    /// client keeps them apart, told by EmeraldBalance, and never lists them with the others. Below every client type,
    /// as <see cref="CurrencyTransaction.Credits"/> is, so no activity-point currency can take it. How habbo.com stores
    /// emeralds is unknown; this is the server's own choice.</summary>
    public const int Emeralds = -2;

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The <see cref="CurrencyType.Type"/>, or <see cref="Emeralds"/>.</summary>
    public int Type { get; set; }

    public int Amount { get; set; }
}
