using Habbo.Domain.Furniture.Chests;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A trade contract: what a user gives, and what they get for it.</summary>
[FurniInteraction("wired_contract_trade")]
public sealed class TradeContractInteraction : ContractInteraction
{
    public override short ContractType => WiredContract.Trade;
}
