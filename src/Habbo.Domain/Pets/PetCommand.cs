namespace Habbo.Domain.Pets;

/// <summary>A command pets can be taught, by the client's id: the level a pet needs to obey it, what
/// obeying earns and costs, and what the pet does.</summary>
public sealed class PetCommand
{
    public int Id { get; set; }

    public PetCommandAction Action { get; set; }

    public int RequiredLevel { get; set; }

    public int Experience { get; set; }

    public int EnergyCost { get; set; }

    /// <summary>The posture it takes, for <see cref="PetCommandAction.Posture"/>.</summary>
    public string? Posture { get; set; }

    /// <summary>How long it holds the posture; 0 until something else happens.</summary>
    public int PostureSeconds { get; set; }
}
