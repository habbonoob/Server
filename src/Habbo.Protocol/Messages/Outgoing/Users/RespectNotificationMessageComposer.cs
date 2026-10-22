using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3848 RespectNotificationMessageComposer — a user was respected; their new total.</summary>
[Message(ServerMessages.RespectNotificationMessageComposer)]
public sealed partial record RespectNotificationMessageComposer(int UserId, int RespectTotal);
