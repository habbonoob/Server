using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>PendingCallForHelpData</summary>
[WireStruct]
public sealed partial record PendingCallForHelpData(string CallId, string TimeStamp, string Message);
