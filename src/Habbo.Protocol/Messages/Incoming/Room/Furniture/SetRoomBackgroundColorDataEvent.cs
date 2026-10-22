using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetRoomBackgroundColorDataEvent — the background toner's colour</summary>
[Incoming(ClientMessages.SetRoomBackgroundColorDataEvent)]
public sealed partial record SetRoomBackgroundColorDataEvent(int ItemId, int Hue, int Saturation, int Lightness);
