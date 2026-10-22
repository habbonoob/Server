using System.Text.Json;
using Habbo.Domain.Rooms.Copies;
using Microsoft.Extensions.Options;

namespace Habbo.Relay;

/// <summary>Saves the rooms the web client's room copier (habbo-pixi's room-copier) copies, where the G-Earth copier
/// (tools/Habbo.RoomCopier) saves its own, named and written as it writes them: the import takes either.</summary>
public sealed class RoomCopies(IOptions<RelayOptions> options)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public string Directory { get; } = Path.GetFullPath(options.Value.RoomCopyDirectory
        ?? Environment.GetEnvironmentVariable("ROOMCOPY_DIR")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Habbo room copies"));

    /// <summary>Writes the copy and says where it went.</summary>
    public async Task<string> SaveAsync(RoomCopy copy, CancellationToken ct)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var name = string.Concat(copy.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Trim();
        var path = Path.Combine(Directory, $"{name} ({copy.SourceRoomId}) {copy.CopiedAt:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(copy, Json), ct);
        return path;
    }
}
