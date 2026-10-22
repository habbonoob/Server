namespace Habbo.Silo.Rooms;

/// <summary>Who may use a kind of hopper: only club members, or only those wearing a costume.</summary>
public sealed class HopperRule
{
    public bool ClubOnly { get; set; }

    public bool CostumeOnly { get; set; }
}
