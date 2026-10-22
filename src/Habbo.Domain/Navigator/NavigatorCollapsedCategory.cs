namespace Habbo.Domain.Navigator;

/// <summary>A result block a player keeps folded in the new navigator.</summary>
public sealed class NavigatorCollapsedCategory
{
    public const int CategoryMaxLength = 64;

    public Guid UserId { get; set; }

    public required string Category { get; set; }
}
