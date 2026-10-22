using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>2585 HabboActivityPointNotificationMessageComposer — one balance moved.</summary>
[Message(ServerMessages.HabboActivityPointNotificationMessageComposer)]
public sealed partial record HabboActivityPointNotificationMessageComposer(int Amount, int Change, int Type);
