using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Notifications;

/// <summary>A parameter of a notification: a key its text or layout reads, and its value.</summary>
[WireStruct]
public sealed partial record NotificationParameterData(string Key, string Value);

/// <summary>2279 NotificationDialogMessageComposer — a notification of a type the client's
/// "notification.&lt;type&gt;" setting describes (a bubble, a pop-up), with its parameters.</summary>
[Message(ServerMessages.NotificationDialogMessageComposer)]
public sealed partial record NotificationDialogMessageComposer(string Type, IReadOnlyList<NotificationParameterData> Parameters);
