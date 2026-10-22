namespace Habbo.Silo.Items.Interactions;

/// <summary>A contract furni: those who may read the room's wired open it to see or write what a wired transaction
/// takes and gives. The room keeps the contract.</summary>
public abstract class ContractInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    /// <summary>The kind of contract, as the client numbers it.</summary>
    public abstract short ContractType { get; }
}
