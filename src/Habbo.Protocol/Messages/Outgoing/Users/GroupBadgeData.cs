using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>GroupBadgeData</summary>
[WireStruct]
public sealed partial record GroupBadgeData(int GroupId, string BadgeCode);
