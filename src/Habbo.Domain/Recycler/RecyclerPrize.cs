using Habbo.Domain.Furniture;

namespace Habbo.Domain.Recycler;

/// <summary>What the Furni-Matic gives at one of its levels: a piece of furniture, or a chat style (habbo.com's rarest
/// prize, October 2026: chat bubble 10001).</summary>
public sealed class RecyclerPrize
{
    public int Id { get; set; }

    public int LevelId { get; set; }

    public int? DefinitionId { get; set; }
    public ItemDefinition? Definition { get; set; }

    public int? ChatStyleId { get; set; }
}
