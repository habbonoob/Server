using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>ModeratorActionResultMessageComposer</summary>
[Message(ServerMessages.ModeratorActionResultMessageComposer)]
public sealed partial record ModeratorActionResultMessageComposer(int UserId, bool Success);
