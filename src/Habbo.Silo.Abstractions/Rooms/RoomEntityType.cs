using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Rooms;

/// <summary>What kind of avatar an entity is — the client's userType discriminator.</summary>
public enum RoomEntityType
{
    User = 1,
    Pet = 2,
    Bot = 3,
    BotWithSkills = 4,
}
