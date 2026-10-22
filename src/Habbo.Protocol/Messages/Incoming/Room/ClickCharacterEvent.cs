using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room;

/// <summary>ClickCharacterEvent</summary>
[Incoming(ClientMessages.ClickCharacterEvent)]
public sealed partial record ClickCharacterEvent(int RoomIndex);
