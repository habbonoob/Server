using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>UserChatlogComposer</summary>
[Message(ServerMessages.UserChatlogComposer)]
public sealed partial record UserChatlogComposer(int UserId, string UserName, IReadOnlyList<ChatRecordData> Rooms);
