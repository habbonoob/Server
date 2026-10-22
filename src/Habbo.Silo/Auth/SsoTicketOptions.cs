namespace Habbo.Silo.Auth;

/// <summary>How SSO tickets behave once issued. Bound from the "Auth" configuration section,
/// which also carries the lifetime the web seam uses when it issues them.</summary>
public sealed class SsoTicketOptions
{
    /// <summary>When true a ticket is burned on first use, so a second login with the same ticket fails.
    /// Turning it off lets one ticket be reused until it expires, which is what local testing wants
    /// when the same client is relaunched over and over.</summary>
    public bool SingleUse { get; set; } = true;
}
