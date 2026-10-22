using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>RoomSettingsErrorComposer</summary>
[Message(ServerMessages.RoomSettingsErrorComposer)]
public sealed partial record RoomSettingsErrorComposer(int RoomId, int ErrorCode);
