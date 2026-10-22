namespace Habbo.Silo.Users;

/// <summary>The sandbox's self donation tool (config section "SelfDonation"): builders give themselves furniture to
/// try wired with. Only a sandbox hotel turns it on.</summary>
public sealed class SelfDonationOptions
{
    public bool Enabled { get; set; }

    /// <summary>The most of one item a donation gives, as the tool allows.</summary>
    public int MaxAmount { get; set; }
}
