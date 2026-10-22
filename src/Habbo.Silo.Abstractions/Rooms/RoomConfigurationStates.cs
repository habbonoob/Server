using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>What a room's configuration furniture switches on: hand items that cannot be passed
/// around, the furni chooser off, furniture moved freely, invisible furniture shown.</summary>
[GenerateSerializer]
public sealed partial record RoomConfigurationStates(
    [property: Id(0)] bool HanditemControlBlocked,
    [property: Id(1)] bool ChooserDisabled,
    [property: Id(2)] bool FreeFurniMovements,
    [property: Id(3)] bool InvisibleFurni);
