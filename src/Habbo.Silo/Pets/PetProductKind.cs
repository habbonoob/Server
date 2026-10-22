namespace Habbo.Silo.Pets;

/// <summary>What a product used on a pet does, by the special type furnidata gives it (the client's
/// furniture category, which picks the confirmation it shows).</summary>
public enum PetProductKind
{
    /// <summary>Colours the pet in another palette of its breed, named by colour tag.</summary>
    Dye = 13,

    /// <summary>Puts parts on layers, keeping the palettes of any already there.</summary>
    Hairstyle = 14,

    /// <summary>Colours the parts on layers, keeping the parts.</summary>
    HairDye = 15,

    /// <summary>Puts a saddle on: parts on layers, and the effect its rider wears.</summary>
    Saddle = 16,

    /// <summary>Brings a dead monster plant back to life.</summary>
    Revival = 20,

    /// <summary>Lets a monster plant breed again, up to the rarity the potion is for.</summary>
    Rebreed = 21,

    /// <summary>Lets a monster plant skip its growing.</summary>
    Fertiliser = 22,
}
