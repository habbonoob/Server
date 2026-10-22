using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>3486 HabboBroadcastMessageComposer — the hotel's alert: the client shows the text in an alert of its own
/// title ("\r" breaks a line).</summary>
[Message(ServerMessages.HabboBroadcastMessageComposer)]
public sealed partial record HabboBroadcastMessageComposer(string Message);
