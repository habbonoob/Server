using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>IssueMessageData — an issue as the mod tool lists it</summary>
[WireStruct]
public sealed partial record IssueMessageData(int IssueId, int State, int CategoryId, int ReportedCategoryId, int IssueAgeInMilliseconds, int Priority, int GroupingId, int ReporterUserId, string ReporterUserName, int ReportedUserId, string ReportedUserName, int PickerUserId, string PickerUserName, string Message, int ChatRecordId, IReadOnlyList<PatternMatchData> Patterns);
