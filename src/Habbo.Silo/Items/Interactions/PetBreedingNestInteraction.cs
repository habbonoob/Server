namespace Habbo.Silo.Items.Interactions;

/// <summary>A breeding nest: two pets of the kind it is for go in, and a baby of its baby kind comes out.
/// Its custom parameters, from furnidata, name the adult kind first and the baby kind last. The room
/// handles the breeding; the nest's states show how many pets are in it.</summary>
[FurniInteraction("pet_breeding_nest")]
public sealed class PetBreedingNestInteraction : FurniInteraction;
