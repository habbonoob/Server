using Habbo.Domain.Pets;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Inventory.Pets;

/// <summary>PetFigureWireData — what a pet looks like: its kind, palette (its breed), colour, and the
/// parts it wears over its own.</summary>
[WireStruct]
public sealed partial record PetFigureWireData(int TypeId, int PaletteId, string Color, int BreedId, IReadOnlyList<PetCustomPartData> CustomParts)
{
    /// <summary>A custom part is kept as layer, part and palette in turn.</summary>
    private const int FieldsPerPart = 3;

    public static PetFigureWireData From(Pet pet) => new(pet.TypeId, pet.PaletteId, pet.Color, pet.PaletteId, Parts(pet.CustomParts));

    public static IReadOnlyList<PetCustomPartData> Parts(IReadOnlyList<int> customParts) =>
        Enumerable.Range(0, customParts.Count / FieldsPerPart)
            .Select(i => new PetCustomPartData(customParts[i * FieldsPerPart], customParts[i * FieldsPerPart + 1], customParts[i * FieldsPerPart + 2]))
            .ToList();

    /// <summary>The same as the figure string the room's Users packet carries: kind, palette, colour, how
    /// many custom parts follow, and each as layer, part and palette.</summary>
    public static string FigureString(Pet pet)
    {
        var parts = Parts(pet.CustomParts);
        return string.Join(' ', new[] { pet.TypeId.ToString(), pet.PaletteId.ToString(), pet.Color, parts.Count.ToString() }
            .Concat(parts.SelectMany(p => new[] { p.LayerId.ToString(), p.PartId.ToString(), p.PaletteId.ToString() })));
    }
}
