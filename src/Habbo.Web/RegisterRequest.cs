namespace Habbo.Web;

/// <summary>A registration: the invite code is needed while the hotel is invite-only, the email address is optional.</summary>
public sealed record RegisterRequest(string? Username, string? Password, string? Email, string? InviteCode);
