namespace Habbo.Silo.Pets;

/// <summary>What a monster plant looks like: its palette, its parts (layer, part and palette in turn), how
/// rare that makes it, and whether breeding mutated it.</summary>
public sealed record PlantLook(int PaletteId, List<int> CustomParts, int Rarity, bool Mutated = false);
