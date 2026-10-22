using System.Globalization;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A football or hockey scoreboard (the client's furniture_hockey_score): its buttons take a goal off
/// (param 1) or add one (2), a double-click clears it (3). Its custom params are its team and its highest score
/// ("3,99"); a goal in its team's goal counts on it too (<see cref="Rooms.FootballPitch"/>).</summary>
[FurniInteraction(Name)]
public sealed class HockeyScoreInteraction : FurniInteraction
{
    public const string Name = "furniture_hockey_score";

    private const int Decrease = 1;
    private const int Increase = 2;
    private const int Clear = 3;

    private const int DefaultTop = 99;

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override Task UseAsync(FurniUse use)
    {
        var score = Score(use.Item);
        var changed = use.Param switch
        {
            Decrease => Math.Max(0, score - 1),
            Increase => Math.Min(Top(use.Item), score + 1),
            Clear => 0,
            _ => score,
        };
        return changed == score ? Task.CompletedTask : use.Room.UpdateAsync(use.Item, use.Item.Data with { State = changed.ToString(CultureInfo.InvariantCulture) });
    }

    public static int Score(Rooms.RoomItemStore.PlacedItem board) =>
        int.TryParse(board.Data.StateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var score) ? score : 0;

    /// <summary>The team its custom params name first, if any.</summary>
    public static int? Team(Rooms.RoomItemStore.PlacedItem board) =>
        int.TryParse(board.Item.Definition!.CustomParams.Split(',')[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var team) ? team : null;

    /// <summary>The highest score it shows, its custom params' second value.</summary>
    public static int Top(Rooms.RoomItemStore.PlacedItem board)
    {
        var parts = board.Item.Definition!.CustomParams.Split(',');
        return parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var top) ? top : DefaultTop;
    }
}
