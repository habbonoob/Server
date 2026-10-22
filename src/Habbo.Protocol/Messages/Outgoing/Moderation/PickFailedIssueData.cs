using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Moderation;

/// <summary>PickFailedIssueData</summary>
[WireStruct]
public sealed partial record PickFailedIssueData(int IssueId, int PickerUserId, string PickerUserName);
