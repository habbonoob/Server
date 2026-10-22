using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Chat;

/// <summary>Chat gestures — indexes into the client's AvatarAction gesture map.</summary>
public enum Gesture
{
    None = 0,
    Smile = 1,
    Aggravated = 2,
    Surprised = 3,
    Sad = 4,
    Joy = 5,
    Crazy = 6,
    Tongue = 7,
    Eyebrows = 8,
    Mischievous = 9,
    Puzzled = 10,
}
