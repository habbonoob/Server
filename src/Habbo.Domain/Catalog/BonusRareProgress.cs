namespace Habbo.Domain.Catalog;

/// <summary>How many credits a player has spent towards the next bonus rare.</summary>
public sealed class BonusRareProgress
{
    public Guid UserId { get; set; }

    public int CreditsSpent { get; set; }
}
