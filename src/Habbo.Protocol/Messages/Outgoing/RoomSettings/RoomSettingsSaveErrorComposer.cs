using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>RoomSettingsSaveErrorComposer</summary>
[Message(ServerMessages.RoomSettingsSaveErrorComposer)]
public sealed partial record RoomSettingsSaveErrorComposer(int RoomId, int ErrorCode, string Info);
