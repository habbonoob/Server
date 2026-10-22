using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Chat;

/// <summary>A chat message as the room broadcasts it; its bubble width forced, or (-1) the room's.</summary>
[GenerateSerializer]
public partial record ChatMessage(
    [property: Id(0)] int RoomIndex,
    [property: Id(1)] string Text,
    [property: Id(2)] Gesture Gesture,
    [property: Id(3)] int StyleId,
    [property: Id(4)] List<ChatLink> Links,
    [property: Id(5)] int TrackingId,
    [property: Id(6)] int BubbleWidth = -1);
