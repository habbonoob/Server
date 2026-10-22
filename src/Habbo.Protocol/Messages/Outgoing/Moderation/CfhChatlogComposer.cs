using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>CfhChatlogComposer</summary>
[Message(ServerMessages.CfhChatlogComposer)]
public sealed partial record CfhChatlogComposer(int CallId, int CallerUserId, int ReportedUserId, int ChatRecordId, ChatRecordData Record);
