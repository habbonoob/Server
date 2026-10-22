using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetMannequinFigureEvent — dress the mannequin in the player's own outfit</summary>
[Incoming(ClientMessages.SetMannequinFigureEvent)]
public sealed partial record SetMannequinFigureEvent(int ItemId);
