using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>CfhReportData</summary>
[WireStruct]
public sealed partial record CfhReportData(long Id, long CreationTime, string UserMessage, int UserCategory, string ReportedAccountName, long CloseTime, bool Sanctioned, bool SanctionGivenByAutoModeration, byte AppealStatus, long AppealCreationTime, long AppealResolutionTime);
