using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Room.Chat;

/// <summary>RoomFilterSettingsMessageComposer</summary>
[Message(ServerMessages.RoomFilterSettingsMessageComposer)]
public sealed partial record RoomFilterSettingsMessageComposer(IReadOnlyList<string> BadWords);
