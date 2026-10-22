namespace Habbo.Web;

/// <summary>An SSO ticket for the client to log in with.</summary>
public sealed record HotelTicket(string SsoTicket, DateTimeOffset ExpiresAt);
