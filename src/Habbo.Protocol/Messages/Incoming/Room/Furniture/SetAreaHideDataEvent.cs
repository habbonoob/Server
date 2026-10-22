using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>SetAreaHideDataEvent — the area a room area hider hides</summary>
[Incoming(ClientMessages.SetAreaHideDataEvent)]
public sealed partial record SetAreaHideDataEvent(int ItemId, int RootX, int RootY, int Width, int Length, bool Invisibility, bool WallItems, bool Invert);
