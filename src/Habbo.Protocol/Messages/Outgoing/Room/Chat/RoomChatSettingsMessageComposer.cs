using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>RoomChatSettingsMessageComposer</summary>
[Message(ServerMessages.RoomChatSettingsMessageComposer)]
public sealed partial record RoomChatSettingsMessageComposer(RoomChatSettingsData ChatSettings);
