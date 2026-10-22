namespace Habbo.Domain.Users;

/// <summary>A word the player filters out of what they read in chat.</summary>
public sealed class UserFilterWord
{
    public const int WordMaxLength = 64;

    public Guid UserId { get; set; }

    public required string Word { get; set; }
}
