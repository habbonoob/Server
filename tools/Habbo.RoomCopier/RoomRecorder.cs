using Habbo.Domain.Rooms.Copies;
using Xabbo.Core;

namespace Habbo.RoomCopier;

/// <summary>Writes down the room the client is in, from what it was sent: the floor plan and door, the walls and
/// floor, and every furni with its place, what it shows and, for the wired boxes that were read, their settings.</summary>
public static class RoomRecorder
{
    /// <param name="className">The class name of a furni type, from the hotel's furnidata, when it was loaded.</param>
    /// <param name="wired">The settings read from wired boxes, by their ids.</param>
    /// <param name="variables">The room's variables, when they were read with the boxes.</param>
    public static RoomCopy Record(RoomCapture room, string hotel, Func<ItemType, int, string?> className,
        IReadOnlyDictionary<long, RoomCopyWired> wired, IReadOnlyList<RoomCopyVariable>? variables) => new(
        RoomCopy.CurrentFormat,
        hotel,
        room.RoomId,
        room.Name.Length > 0 ? room.Name : $"Room {room.RoomId}",
        room.Description,
        DateTimeOffset.UtcNow,
        room.Heightmap ?? "",
        room.Door.X,
        room.Door.Y,
        room.Door.Direction,
        room.FixedWallsHeight,
        room.UseSmallScale,
        room.WallThickness,
        room.FloorThickness,
        room.WallsHidden,
        room.Wallpaper,
        room.Floor,
        room.Landscape,
        room.FloorItems.Where(item => !item.IsHidden).Select(item => new RoomCopyFloorItem(
            item.Kind, Named(item) ?? className(ItemType.Floor, item.Kind), item.X, item.Y, item.Z, item.Direction,
            Data(item.Data), (long)item.Id, wired.GetValueOrDefault((long)item.Id))).ToList(),
        room.WallItems.Where(item => !item.IsHidden).Select(item => new RoomCopyWallItem(
            item.Kind, Named(item) ?? className(ItemType.Wall, item.Kind), item.Location.ToString(), item.Data ?? "",
            (long)item.Id)).ToList(),
        variables);

    /// <summary>The class name, which the Unity client is sent and Flash is not.</summary>
    private static string? Named(IItem item) => string.IsNullOrEmpty(item.Identifier) ? null : item.Identifier;

    /// <summary>The item's data in its own layout. The limited-edition numbers are left behind: a copy is not one of the
    /// run.</summary>
    private static RoomCopyItemData Data(IItemData data)
    {
        var plain = new RoomCopyItemData((int)data.Type, data.Value, null, null, null, null, null, null, null, null, null);
        return data switch
        {
            IMapData map => plain with { State = null, Map = map.ToDictionary(entry => entry.Key, entry => entry.Value) },
            IStringArrayData strings => plain with { State = null, Strings = strings.ToList() },
            IIntArrayData ints => plain with { State = null, Ints = ints.ToList() },
            IVoteResultData vote => plain with { VoteResult = vote.Result },
            IHighScoreData scores => plain with
            {
                ScoreType = scores.ScoreType,
                ClearType = scores.ClearType,
                HighScores = scores.Select(score => new RoomCopyHighScore(score.Score, score.Users.ToList())).ToList(),
            },
            ICrackableFurniData crackable => plain with { Hits = crackable.Hits, Target = crackable.Target },
            _ => plain,
        };
    }
}
