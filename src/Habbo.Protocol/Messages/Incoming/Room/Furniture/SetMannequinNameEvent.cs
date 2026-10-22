using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetMannequinNameEvent</summary>
[Incoming(ClientMessages.SetMannequinNameEvent)]
public sealed partial record SetMannequinNameEvent(int ItemId, string Name);
