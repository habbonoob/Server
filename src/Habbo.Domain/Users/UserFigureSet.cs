namespace Habbo.Domain.Users;

/// <summary>A clothing set a player has unlocked by redeeming a clothing furni, and which furni it
/// came from: the avatar editor lists both, the sets to wear and the furni names to credit.</summary>
public sealed class UserFigureSet
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The set's id in figuredata.</summary>
    public int SetId { get; set; }

    /// <summary>The class name of the clothing furni that unlocked it.</summary>
    public string FurniClassName { get; set; } = string.Empty;
}
