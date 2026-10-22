using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Chat;

public interface IGestureCatalog
{
    Task<Gesture> DetectAsync(string text, CancellationToken ct = default);
}
