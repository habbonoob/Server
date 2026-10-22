using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>RoomModerationSettingsData</summary>
[WireStruct]
public sealed partial record RoomModerationSettingsData(int WhoCanMute, int WhoCanKick, int WhoCanBan);
