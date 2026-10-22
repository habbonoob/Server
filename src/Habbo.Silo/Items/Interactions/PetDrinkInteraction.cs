using System.Globalization;
using Habbo.Silo.Abstractions.Furni;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A pet's water bowl or trough: pets drink from it a state at a time until it is empty, and
/// using it fills it up again.</summary>
[FurniInteraction("pet_drink")]
public sealed class PetDrinkInteraction : FurniInteraction
{
    /// <summary>The state of a full bowl.</summary>
    public const int Full = 0;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use) =>
        use.Action == FurniAction.Use && use.Item.Data.State != Full.ToString(CultureInfo.InvariantCulture)
            ? use.Room.UpdateAsync(use.Item, use.Item.Data with { State = Full.ToString(CultureInfo.InvariantCulture) })
            : Task.CompletedTask;
}
