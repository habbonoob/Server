using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>ClubGiftNotificationComposer — club gifts the member has earned and not yet chosen</summary>
[Message(ServerMessages.ClubGiftNotificationComposer)]
public sealed partial record ClubGiftNotificationComposer(int NumGifts);
