using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>MyCfhReportStatusMessageComposer</summary>
[Message(ServerMessages.MyCfhReportStatusMessageComposer)]
public sealed partial record MyCfhReportStatusMessageComposer(IReadOnlyList<CfhReportData> Messages);
