using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>RoomChatlogComposer</summary>
[Message(ServerMessages.RoomChatlogComposer)]
public sealed partial record RoomChatlogComposer(ChatRecordData Record);
