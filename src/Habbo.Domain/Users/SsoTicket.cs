namespace Habbo.Domain.Users;

/// <summary>SSO ticket issued by the web seam, presented by the client on connect, validated by the silo. Single use
/// and short lived, unless permanent.</summary>
public sealed class SsoTicket
{
    public required string Ticket { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }

    /// <summary>A ticket that neither expires nor is used up: a development account's, set in its seed settings, so a
    /// client relaunched over and over logs in with the same one. Anyone holding it logs in as the account.</summary>
    public bool Permanent { get; set; }
}
