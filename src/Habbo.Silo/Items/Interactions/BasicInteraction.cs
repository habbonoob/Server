
using Habbo.Domain.Furniture;

namespace Habbo.Silo.Items.Interactions;

/// <summary>Furniture that just stands there. Also what any item gets whose behaviour the
/// server does not implement yet.</summary>
[FurniInteraction(ItemDefinition.BasicLogic)]
public sealed class BasicInteraction : FurniInteraction;
