using Habbo.Domain.Rooms;
using Habbo.Domain.Users;

namespace Habbo.Domain.Pets;

/// <summary>A pet a player owns: in their inventory, or in one of their rooms. Energy and nutrition
/// are as they stood at <see cref="VitalsAt"/>; time has moved them on since.</summary>
public sealed class Pet
{
    public const int NameMaxLength = 32;
    public const int ColorLength = 6;

    public int Id { get; set; }

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public int TypeId { get; set; }
    public PetType? Type { get; set; }

    /// <summary>Its palette, which is its breed.</summary>
    public int PaletteId { get; set; }

    /// <summary>The colour it was bought in, as six hex digits.</summary>
    public string Color { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Level { get; set; } = 1;

    public int Experience { get; set; }

    public int Energy { get; set; }

    public int Nutrition { get; set; }

    public DateTimeOffset VitalsAt { get; set; }

    /// <summary>How often it has been respected.</summary>
    public int Respect { get; set; }

    /// <summary>The room it is in; null while it is in the inventory.</summary>
    public int? RoomId { get; set; }
    public Room? Room { get; set; }

    public int X { get; set; }
    public int Y { get; set; }
    public int Rotation { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Parts worn over its own, as layer, part and palette in turn: a hairstyle, a saddle.</summary>
    public List<int> CustomParts { get; set; } = [];

    /// <summary>The saddle it wears, which goes back to its owner when taken off.</summary>
    public int? SaddleDefinitionId { get; set; }

    /// <summary>Anyone may ride it, not only its owner.</summary>
    public bool AnyoneCanRide { get; set; }

    /// <summary>Its owner lets it be bred with others' pets.</summary>
    public bool BreedingAllowed { get; set; }

    /// <summary>For a monster plant: how rare its look is (0-10).</summary>
    public int Rarity { get; set; }

    /// <summary>For a monster plant: whether it may still breed; a plant breeds once, until it is given a
    /// rebreeding potion.</summary>
    public bool CanBreed { get; set; } = true;

    /// <summary>For a monster plant: until when it is well, after which it dies; and when it died.</summary>
    public DateTimeOffset? WellUntil { get; set; }
    public DateTimeOffset? DiedAt { get; set; }

    /// <summary>For a monster plant: how much of its growing it has been spared, by light and fertiliser.</summary>
    public int GrowthSkippedSeconds { get; set; }
}
