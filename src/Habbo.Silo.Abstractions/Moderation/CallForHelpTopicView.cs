using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>One report reason. The consequence is what the client warns the reporter with.</summary>
[GenerateSerializer]
public partial record CallForHelpTopicView(
    [property: Id(0)] string Name,
    [property: Id(1)] int Id,
    [property: Id(2)] string Consequence);
