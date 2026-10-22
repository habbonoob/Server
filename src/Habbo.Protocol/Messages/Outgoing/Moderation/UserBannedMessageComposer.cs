using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>UserBannedMessageComposer</summary>
[Message(ServerMessages.UserBannedMessageComposer)]
public sealed partial record UserBannedMessageComposer(string Message);
