namespace Habbo.Silo.Items.Interactions;

/// <summary>A furni chest: it stores furniture.</summary>
[FurniInteraction("furniture_furnichest")]
public sealed class FurniChestInteraction : ChestInteraction
{
    public override bool HoldsCoins => false;
}
