using Habbo.Protocol.Messages.Outgoing.Inventory.Trading;
using Habbo.Silo.Chests;

namespace Habbo.Silo.Rooms;

/// <summary>A player trading with wired: depositing into a chest, or paying or trading under a contract. What they
/// offer stays in their inventory until they accept and then confirm; what wired offers stays in its chests.</summary>
public sealed class WiredTrade(Guid playerId, int chestId, int requirementType, ContractTerms? terms = null)
{
    public Guid PlayerId { get; } = playerId;

    /// <summary>The chest a deposit goes into; unused under a contract.</summary>
    public int ChestId { get; } = chestId;

    /// <summary>What the trade asks for, as the client numbers it.</summary>
    public int RequirementType { get; } = requirementType;

    /// <summary>The contract the trade runs under; none for a deposit.</summary>
    public ContractTerms? Terms { get; } = terms;

    /// <summary>Tells this trade apart from one that replaced it, for a timeout set on the old one.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The offered items by id, in the order they were put in.</summary>
    public Dictionary<int, TradeItemData> Items { get; } = [];

    /// <summary>The credits each offered credit furni is worth, by item id.</summary>
    public Dictionary<int, int> CreditValues { get; } = [];

    /// <summary>The kind of each offered item that is not credit furni, by item id.</summary>
    public Dictionary<int, ChestKind> Kinds { get; } = [];

    /// <summary>How many times the offer meets the contract's rules; 0 while it does not.</summary>
    public int TimesMet { get; set; }

    /// <summary>What wired gives for it under a trade contract: items held in its chests, and credits.</summary>
    public List<(int ChestId, HeldItem Item)> Reserved { get; } = [];
    public List<TradeItemData> ReservedView { get; } = [];
    public int ReservedCredits { get; set; }

    public bool Accepted { get; set; }

    public int Credits => CreditValues.Values.Sum();
}
