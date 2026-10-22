namespace Habbo.Silo.Users;

/// <summary>One season of a seasonal currency: while it runs the currency is paid out; when it ends, every balance of
/// it goes back to nothing.</summary>
public sealed class CurrencySeason
{
    public int Type { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public bool IsRunning(DateTimeOffset now) => StartsAt <= now && now < EndsAt;
}
