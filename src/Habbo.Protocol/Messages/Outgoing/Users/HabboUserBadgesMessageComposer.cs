using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>3069 HabboUserBadgesMessageComposer — the badges a user wears.</summary>
[Message(ServerMessages.HabboUserBadgesMessageComposer)]
public sealed partial record HabboUserBadgesMessageComposer(int UserId, IReadOnlyList<WornBadgeData> Badges)
{
    public static HabboUserBadgesMessageComposer From(int accountId, IReadOnlyList<WornBadge> worn) =>
        new(accountId, worn.Select(b => new WornBadgeData(b.Slot, b.Code, b.Owners, b.Rarity)).ToList());
}
