using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.RoomSettings;

/// <summary>GetRoomSettingsMessageEvent</summary>
[Incoming(ClientMessages.GetRoomSettingsMessageEvent)]
public sealed partial record GetRoomSettingsMessageEvent(int RoomId);
