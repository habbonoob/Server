using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Protocol.Messages.Outgoing.Room.Session;

/// <summary>ConfigurationItemStatesMessageComposer</summary>
[Message(ServerMessages.ConfigurationItemStatesMessageComposer)]
public sealed partial record ConfigurationItemStatesMessageComposer(bool IsHanditemControlBlocked, bool ChooserDisabled, bool FreeFurniMovementsEnabled, bool InvisibleFurni)
{
    public static ConfigurationItemStatesMessageComposer From(RoomConfigurationStates states) => new(
        states.HanditemControlBlocked, states.ChooserDisabled, states.FreeFurniMovements, states.InvisibleFurni);
}
