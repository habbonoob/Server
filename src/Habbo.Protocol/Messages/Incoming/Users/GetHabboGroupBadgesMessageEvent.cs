using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Users;

/// <summary>GetHabboGroupBadgesMessageEvent — the badges of the groups the client meets in the room</summary>
[Incoming(ClientMessages.GetHabboGroupBadgesMessageEvent)]
public sealed partial record GetHabboGroupBadgesMessageEvent;
