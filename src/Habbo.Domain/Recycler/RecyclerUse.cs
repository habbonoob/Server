namespace Habbo.Domain.Recycler;

/// <summary>When a player last put furniture through the Furni-Matic, which it waits after.</summary>
public sealed class RecyclerUse
{
    public Guid UserId { get; set; }

    public DateTimeOffset LastRecycledAt { get; set; }
}
