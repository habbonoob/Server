using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Friends;

[GenerateSerializer]
public partial record FriendCategoryView([property: Id(0)] int Id, [property: Id(1)] string Name);
