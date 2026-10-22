using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Inventory.Furni;

/// <summary>693 RequestRoomPropertySet — put a wallpaper, floor or landscape from the inventory down in the room. The
/// item is named by its strip id.</summary>
[Incoming(ClientMessages.RequestRoomPropertySetEvent)]
public sealed partial record RequestRoomPropertySetEvent(int StripId);
