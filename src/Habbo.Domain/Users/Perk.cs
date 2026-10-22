namespace Habbo.Domain.Users;

/// <summary>A feature the client gates on, sent to it in PerkAllowancesMessageComposer.
/// Whether a player gets it is decided by rank, so enabling a perk for staff is a data
/// change rather than a code change.</summary>
public sealed class Perk
{
    /// <summary>The code the client knows the feature by, e.g. NAVIGATOR_PHASE_ONE_2014.</summary>
    public required string Code { get; set; }

    /// <summary>Shown by the client when a player uses the feature without being allowed to.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>The lowest <see cref="User.Rank"/> that gets this perk. None means everyone.</summary>
    public SecurityLevel MinimumRank { get; set; }
}
