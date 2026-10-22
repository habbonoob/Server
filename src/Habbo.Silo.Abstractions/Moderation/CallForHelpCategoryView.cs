using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>One group of report reasons, as the silo hands it to the gateway.</summary>
[GenerateSerializer]
public partial record CallForHelpCategoryView(
    [property: Id(0)] string Name,
    [property: Id(1)] IReadOnlyList<CallForHelpTopicView> Topics);
