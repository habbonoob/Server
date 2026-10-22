using Habbo.Domain.Furniture.Chests;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A reward contract: what a user is given.</summary>
[FurniInteraction("wired_contract_reward")]
public sealed class RewardContractInteraction : ContractInteraction
{
    public override short ContractType => WiredContract.Reward;
}
