using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>ReportedChatLineData — a chat line the reporter picked</summary>
[WireStruct]
public sealed partial record ReportedChatLineData(int UserId, string Text);
