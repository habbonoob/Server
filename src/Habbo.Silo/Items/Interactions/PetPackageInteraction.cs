namespace Habbo.Silo.Items.Interactions;

/// <summary>A box with a pet in it: its owner opens it by naming the pet, which then stands where the box
/// stood. The room handles the opening, since it is a pet and not the item that comes out. The client offers its use
/// button to those with rights, as habbo.com's box shows it; only the owner gets the pet.</summary>
[FurniInteraction("pet_package")]
public sealed class PetPackageInteraction : FurniInteraction
{
    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;
}
