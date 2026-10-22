using Habbo.Domain.Friends;
using Habbo.Domain.Achievements;
using Habbo.Domain.Rooms;
using Habbo.Domain.Users;
using Orleans;

namespace Habbo.Silo.Abstractions.Moderation;

/// <summary>Reads the reasons a player may report something for. The client has no list of
/// its own, so whatever this returns is the whole of what the help tool offers.</summary>
public interface ICallForHelpService
{
    Task<IReadOnlyList<CallForHelpCategoryView>> GetCategoriesAsync(CancellationToken ct = default);
}
