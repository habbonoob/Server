using Habbo.Domain.Furniture;
using Habbo.Domain.Furniture.Data;
using Habbo.Silo.Abstractions.Furni;
using Habbo.Silo.Rooms;

namespace Habbo.Silo.Items.Interactions;

/// <summary>A high score board: the room's games write their results on it when they end. What it scores and when it
/// clears are in its name — highscore_classic*2 is a classic board cleared daily. Switching it on shows the board.</summary>
[FurniInteraction("furniture_high_score")]
public sealed class HighScoreInteraction : FurniInteraction
{
    private const string ClassPrefix = "highscore_";
    private const char VariantSeparator = '*';

    /// <summary>The boards' names for the client's score types, in its order (HighScoreDisplayWidget).</summary>
    private static readonly string[] ScoreTypeNames = ["perteam", "mostwin", "classic", "fastesttime", "longesttime"];

    private const string Off = "0";
    private const string On = "1";

    public override FurniUsagePolicy UsagePolicy => FurniUsagePolicy.Controllers;

    public override void Grant(FurniGrant grant) => FurniDataStore.Attach(grant.Db, grant.Item, Blank(grant.Definition));

    public override Task UseAsync(FurniUse use)
    {
        if (use.Action != FurniAction.Use)
            return Task.CompletedTask;

        var data = BoardOf(use.Item);
        return use.Room.UpdateAsync(use.Item, data with { State = data.State == On ? Off : On });
    }

    /// <summary>The board's data; a board that has none yet starts empty, switched off.</summary>
    public static FurniDataView BoardOf(RoomItemStore.PlacedItem item) =>
        item.Data.Type == (int)FurniDataType.HighScore ? item.Data : Blank(item.Item.Definition!) with { State = item.Data.State ?? Off };

    private static FurniDataView Blank(ItemDefinition definition)
    {
        var name = definition.ClassName.StartsWith(ClassPrefix, StringComparison.Ordinal) ? definition.ClassName[ClassPrefix.Length..] : string.Empty;
        var separator = name.IndexOf(VariantSeparator);
        var scoreType = Array.IndexOf(ScoreTypeNames, separator < 0 ? name : name[..separator]);
        var variant = separator < 0 ? 1 : int.TryParse(name[(separator + 1)..], out var parsed) ? parsed : 1;
        return FurniDataView.Plain with
        {
            Type = (int)FurniDataType.HighScore,
            State = Off,
            ScoreType = Math.Max(0, scoreType),
            ClearType = Math.Clamp(variant - 1, HighScoreBoards.AllTime, HighScoreBoards.Monthly),
            HighScoreEntries = [],
        };
    }
}
