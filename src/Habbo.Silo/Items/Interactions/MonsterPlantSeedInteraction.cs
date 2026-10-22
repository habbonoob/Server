namespace Habbo.Silo.Items.Interactions;

/// <summary>A monster plant seed: its owner plants it by using it, and a plant grows where it lay. Its
/// custom parameters, from furnidata, are the least rarity the plant will have. The room does the planting.</summary>
[FurniInteraction("furniture_monsterplant_seed")]
public sealed class MonsterPlantSeedInteraction : FurniInteraction;
