using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>3318 InfoFeedEnableMessageComposer — whether the client shows the info feed, the strip
/// of hotel notices beside the chat. A player preference, not a hotel setting.</summary>
[Message(ServerMessages.InfoFeedEnableMessageComposer)]
public sealed partial record InfoFeedEnableMessageComposer(bool Enabled);
