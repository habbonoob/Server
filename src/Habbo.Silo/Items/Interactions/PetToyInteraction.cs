namespace Habbo.Silo.Items.Interactions;

/// <summary>A pet's toy: pets play with it; its states are its looks, stepped through on use.</summary>
[FurniInteraction("pet_toy")]
public sealed class PetToyInteraction : FurniInteraction
{
    private readonly MultiStateInteraction _states = new();

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Everyone;

    public override Task UseAsync(FurniUse use) => _states.UseAsync(use);
}
