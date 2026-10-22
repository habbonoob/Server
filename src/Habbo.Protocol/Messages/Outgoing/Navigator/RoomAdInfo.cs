using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Navigator;

/// <summary>A room's promotion. Present only when GuestRoomData sets bit 0x04.</summary>
[WireStruct]
public sealed partial record RoomAdInfo(string Name, string Description, int ExpiresInMinutes);
