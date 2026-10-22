namespace Habbo.Domain.Users;

/// <summary>What a player holds of one activity-point currency.</summary>
public sealed class UserCurrency
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The <see cref="CurrencyType.Type"/>.</summary>
    public int Type { get; set; }

    public int Amount { get; set; }
}
