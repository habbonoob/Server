using Habbo.Domain.Furniture.Chests;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A payment contract: what a user pays.</summary>
[FurniInteraction("wired_contract_payment")]
public sealed class PaymentContractInteraction : ContractInteraction
{
    public override short ContractType => WiredContract.Payment;
}
