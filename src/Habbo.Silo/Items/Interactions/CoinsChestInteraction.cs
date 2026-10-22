namespace Habbo.Silo.Items.Interactions;

/// <summary>A credit chest: it stores credits, paid in with credit furni.</summary>
[FurniInteraction("furniture_coinschest")]
public sealed class CoinsChestInteraction : ChestInteraction
{
    public override bool HoldsCoins => true;
}
