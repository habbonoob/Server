namespace Habbo.Silo.Rooms.Copies;

/// <summary>What an import built: the room, how many furni went in, and the furni types this hotel doesn't have, by
/// how many of each were left out (keyed by class name, or "floor:&lt;id&gt;"/"wall:&lt;id&gt;" when the copy didn't
/// name them), and how many wired boxes were set up as they were. <see cref="RefusedWired"/> are the boxes placed
/// with nothing set, by class name: a save here would have refused their settings, or needs a permission the importer
/// lacks. <see cref="Errors"/> are the floor plan's problems, in the editor's keys; then nothing was built.</summary>
public sealed record RoomImportResult(
    int? RoomId,
    string Name,
    int FloorItems,
    int WallItems,
    IReadOnlyDictionary<string, int> Missing,
    IReadOnlyList<string> Errors,
    int WiredBoxes = 0,
    IReadOnlyDictionary<string, int>? RefusedWired = null);
