namespace Habbo.Domain.Navigator;

/// <summary>A search a player saved to the new navigator's side pane.</summary>
public sealed class SavedNavigatorSearch
{
    public const int SearchCodeMaxLength = 64;

    public const int FilterMaxLength = 128;

    public int Id { get; set; }

    public Guid UserId { get; set; }

    public required string SearchCode { get; set; }

    public string Filter { get; set; } = string.Empty;
}
