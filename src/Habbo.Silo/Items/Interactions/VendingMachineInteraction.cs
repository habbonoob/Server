namespace Habbo.Silo.Items.Interactions;

/// <summary>A vending machine, or anything else that hands over something to carry: a drink, a book, a compass.</summary>
[FurniInteraction("vending_machine")]
public sealed class VendingMachineInteraction : DispenserInteraction
{
    protected override void Give(FurniUse use, int id) => use.Room.SetHandItem(use.User, id);
}
