namespace Habbo.Web;

/// <summary>A request for an invite code, with a note of who it is for.</summary>
public sealed record NewInvite(string? Note);
