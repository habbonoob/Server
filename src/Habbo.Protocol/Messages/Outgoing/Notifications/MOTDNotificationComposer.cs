using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>3706 MOTDNotificationComposer.</summary>
[Message(ServerMessages.MOTDNotificationComposer)]
public sealed partial record MOTDNotificationComposer(IReadOnlyList<string> Messages);
