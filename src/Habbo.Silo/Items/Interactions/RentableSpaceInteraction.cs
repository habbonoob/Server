namespace Habbo.Silo.Items.Interactions;

/// <summary>A space for rent: whoever rents it may decorate the tiles it stands on (or the whole room, as
/// its custom parameters say) until the rent runs out, when what they put there goes back to them. The
/// room keeps the renting.</summary>
[FurniInteraction("furniture_rentable_space")]
public sealed class RentableSpaceInteraction : FurniInteraction;
