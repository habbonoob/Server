using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.CallForHelp;

/// <summary>CfhSanctionMessageComposer</summary>
[Message(ServerMessages.CfhSanctionMessageComposer)]
public sealed partial record CfhSanctionMessageComposer(int IssueId, int AccountId, string Name, int SanctionLengthInHours, int ProbationDays, bool AvatarOnly, string TradeLockInfo, string MachineBanInfo);
