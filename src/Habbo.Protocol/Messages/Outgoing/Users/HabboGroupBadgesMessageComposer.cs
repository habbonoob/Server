using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>HabboGroupBadgesMessageComposer</summary>
[Message(ServerMessages.HabboGroupBadgesMessageComposer)]
public sealed partial record HabboGroupBadgesMessageComposer(IReadOnlyList<GroupBadgeData> Badges);
