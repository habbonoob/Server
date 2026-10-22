using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Chat;

/// <summary>A hyperlink attached to a chat message.</summary>
[GenerateSerializer]
public partial record ChatLink(
    [property: Id(0)] string Text,
    [property: Id(1)] string Url,
    [property: Id(2)] bool Trusted);
