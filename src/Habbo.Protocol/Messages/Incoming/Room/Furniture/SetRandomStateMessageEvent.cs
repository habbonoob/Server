using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>3152 SetRandomState — a double-click on a furniture_randomstate item, such as a
/// fortune wheel. The server picks the state.</summary>
[Incoming(ClientMessages.SetRandomStateMessageEvent)]
public sealed partial record SetRandomStateMessageEvent(int ItemId, int Param);
