using Habbo.Domain.Furniture;

namespace Habbo.Domain.Recycler;

/// <summary>A piece of furniture the Furni-Matic gives at one of its levels.</summary>
public sealed class RecyclerPrize
{
    public int Id { get; set; }

    public int LevelId { get; set; }

    public int DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }
}
