namespace Habbo.Silo.Chests;

/// <summary>What a chest in the room holds, kept alongside the database so wired can count it at once.</summary>
public sealed class ChestHold
{
    public int Coins { get; set; }

    public List<HeldItem> Items { get; } = [];
}
