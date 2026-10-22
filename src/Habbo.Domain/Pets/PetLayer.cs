namespace Habbo.Domain.Pets;

/// <summary>A layer a kind of pet is drawn in, by its tag: "body", "hair", "saddle".</summary>
public sealed class PetLayer
{
    public int TypeId { get; set; }

    public int LayerId { get; set; }

    public string Tag { get; set; } = string.Empty;
}
