using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Habbicons;

/// <summary>A Habbicon the player owns, and whether it is starred (<see cref="HabbiconState.Owned"/> or
/// <see cref="HabbiconState.Favourite"/>).</summary>
[WireStruct]
public sealed partial record UserHabbiconData(int HabbiconId, HabbiconState State);
