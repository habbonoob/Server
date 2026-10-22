namespace Habbo.Web;

/// <summary>An invite code as staff see it: who it was for, and who used it.</summary>
public sealed record InviteView(string Code, string Note, DateTimeOffset CreatedAt, string? UsedBy, DateTimeOffset? UsedAt);
