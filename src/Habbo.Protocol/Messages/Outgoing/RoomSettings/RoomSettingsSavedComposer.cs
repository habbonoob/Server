using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.RoomSettings;

/// <summary>RoomSettingsSavedComposer</summary>
[Message(ServerMessages.RoomSettingsSavedComposer)]
public sealed partial record RoomSettingsSavedComposer(int RoomId);
