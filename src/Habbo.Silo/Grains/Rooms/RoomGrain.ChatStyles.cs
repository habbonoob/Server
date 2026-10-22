using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Grains.Rooms;

/// <summary>The chat bubble a line is said in: the one the client asks for, if the user may use it.</summary>
public sealed partial class RoomGrain
{
    /// <summary>The plain bubble, which anyone may chat in.</summary>
    private const int PlainChatStyle = 0;

    /// <summary>How long a bubble refused stays refused before it is asked about again: one bought meanwhile
    /// becomes usable without leaving the room, and a client sending one over and over is not a query a line.</summary>
    private static readonly TimeSpan RefusedChatStyleFor = TimeSpan.FromMinutes(1);

    /// <summary>The bubble to say a line in: the client's choice when the user may use it (staff, club,
    /// owned and system bubbles are checked as the selector checks them), otherwise the plain one.</summary>
    private async Task<int> ChatStyleAsync(RoomEntityStore.RoomEntity entity, int styleId)
    {
        if (styleId == PlainChatStyle)
            return styleId;

        var now = DateTimeOffset.UtcNow;
        if (!entity.ChatStyles.TryGetValue(styleId, out var known) || (!known.Allowed && now - known.CheckedAt > RefusedChatStyleFor))
        {
            known = (await GrainFactory.GetGrain<IPlayerGrain>(entity.Player.Id).CanUseChatStyleAsync(styleId), now);
            entity.ChatStyles[styleId] = known;
        }

        return known.Allowed ? styleId : PlainChatStyle;
    }
}
