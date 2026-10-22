namespace Habbo.Silo.Users;

/// <summary>How users are classified for staff and ambassadors (config section "UserClassification").</summary>
public sealed class UserClassificationOptions
{
    /// <summary>The class of an account younger than <see cref="NewAccountDays"/>; ambassadors may list these.</summary>
    public string NewClass { get; set; } = string.Empty;
    public int NewAccountDays { get; set; }

    /// <summary>The class of everyone else.</summary>
    public string RegularClass { get; set; } = string.Empty;
}
