using System.Text.Json;
using Xabbo.Core;

namespace Habbo.RoomCopier;

/// <summary>The class names of the hotel's furni by their furnidata ids, which the Flash client is not sent in a room:
/// read from the hotel's own furnidata (gamedata/furnidata_json), as the client downloads it. xabbo's game data
/// loader would do it, but it stops at a game data kind newer than it.</summary>
public sealed class FurniNames
{
    private readonly Dictionary<(ItemType, int), string> _names = [];

    public int Count => _names.Count;

    public string? For(ItemType type, int id) => _names.GetValueOrDefault((type, id));

    public static async Task<FurniNames> LoadAsync(string webHost, CancellationToken ct = default)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36");
        await using var stream = await http.GetStreamAsync($"https://{webHost}/gamedata/furnidata_json/1", ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var names = new FurniNames();
        names.Add(document.RootElement, "roomitemtypes", ItemType.Floor);
        names.Add(document.RootElement, "wallitemtypes", ItemType.Wall);
        return names;
    }

    private void Add(JsonElement root, string section, ItemType type)
    {
        foreach (var furni in root.GetProperty(section).GetProperty("furnitype").EnumerateArray())
            _names[(type, furni.GetProperty("id").GetInt32())] = furni.GetProperty("classname").GetString() ?? "";
    }
}
