namespace Habbo.Domain.Users;

/// <summary>One movement of a player's money: credits or an activity-point currency, in or out,
/// and why. The club's payday pays back a share of what was spent, so spending has to be on
/// record rather than only netted into the balance.</summary>
public sealed class CurrencyTransaction
{
    /// <summary>The <see cref="Currency"/> credits are recorded under; activity points use their own type.</summary>
    public const int Credits = -1;

    public const int ReasonMaxLength = 64;

    public long Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary><see cref="Credits"/>, or a <see cref="CurrencyType.Type"/>.</summary>
    public int Currency { get; set; }

    /// <summary>Positive when the player received it, negative when they spent it.</summary>
    public int Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
