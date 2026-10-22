using System.Buffers.Binary;
using System.Text.Json;
using Habbo.Domain.Rooms.Copies;
using Habbo.RoomCopier;
using Xabbo;
using Xabbo.Core;
using Xabbo.Core.Messages.Incoming;
using Xabbo.GEarth;
using Xabbo.Messages;

// A G-Earth extension that copies the room you are in to a file, for our hotel's room import to build again. :copy
// (or G-Earth's green play button) only reads what the client was sent. :copy wired also opens each wired box, one
// at a time as a double click would, to read its settings; the dialogs it asks for never show. Neither is said in
// the room. See README.md.

var directory = Path.GetFullPath(Environment.GetEnvironmentVariable("ROOMCOPY_DIR") ?? Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Habbo room copies"));
Directory.CreateDirectory(directory);

// a box asked for every so often, and given this long to answer
var askEvery = TimeSpan.FromMilliseconds(400);
var answerWithin = TimeSpan.FromSeconds(3);
string[] boxPrefixes = ["wf_trg_", "wf_act_", "wf_cnd_", "wf_slc_", "wf_xtra_", "wf_var_"];

var ext = new GEarthExtension(new GEarthOptions
{
    Name = "Room copier",
    Description = "Copies the room you are in for import into our hotel (:copy, :copy wired, or the play button)",
    Author = "habbo-emulator",
    Version = "1.1",
});
var room = new RoomCapture();
var wiredReader = new WiredReader();
FurniNames? furniNames = null;
var hotel = "";
var copying = 0;
var json = new JsonSerializerOptions { WriteIndented = true };
// ROOMCOPY_DEBUG=1 prints each packet name the first time it comes, to see what the client is sent
var debug = Environment.GetEnvironmentVariable("ROOMCOPY_DEBUG") == "1";
var seen = new HashSet<string>();

ext.Connected += e =>
{
    hotel = e.Session.Hotel.Domain;
    Console.WriteLine($"Connected to {e.Session.Hotel}. Copies go to {directory}");
    // the furni's class names, which the Flash client is not sent: they name the copy's furni and find its wired
    _ = Task.Run(async () =>
    {
        try
        {
            furniNames = await FurniNames.LoadAsync(e.Session.Hotel.WebHost);
            Console.WriteLine($"Furnidata loaded: {furniNames.Count} furni.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not load the furnidata, so copies have no class names and no wired: {ex.Message}");
        }
    });
};
ext.Disconnected += () => Console.WriteLine("Game disconnected.");
ext.Activated += () => Start(withWired: false);

ext.Intercepted += e =>
{
    var name = ext.Messages.TryGetNames(e.Packet.Header, out MessageNames names)
        ? names.GetName(ext.Session.Client.Type)
        : null;
    if (name is null)
        return;
    if (debug && seen.Add($"{e.Direction} {name}"))
        Console.WriteLine($"[{e.Direction}] {name}");

    if (e.Direction == Direction.In)
    {
        wiredReader.ReadVariables(name, e.Packet);
        if (wiredReader.TryTake(name, e.Packet))
            e.Block();
        else
            room.Read(name, e.Packet);
        return;
    }

    // :copy and :copy wired are for the copier, not said out loud
    if (name is "Chat" or "Shout" or "Whisper")
    {
        var position = e.Packet.Position;
        var said = e.Packet.Read<string>().Trim();
        e.Packet.Position = position;
        if (said.Equals(":copy", StringComparison.OrdinalIgnoreCase) || said.Equals(":copy wired", StringComparison.OrdinalIgnoreCase))
        {
            e.Block();
            Start(withWired: said.Length > ":copy".Length);
        }
    }
};

Console.WriteLine("Room copier: waiting for G-Earth.");
await ext.RunAsync();

// off the packet thread: reading the wired waits on answers that come through it
void Start(bool withWired)
{
    if (Interlocked.Exchange(ref copying, 1) == 1)
    {
        Tell("Still copying the last one.");
        return;
    }
    _ = Task.Run(async () =>
    {
        try
        {
            await CopyAsync(withWired);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"The copy failed: {ex}");
            Tell($"The copy failed: {ex.Message}");
        }
        finally
        {
            Volatile.Write(ref copying, 0);
        }
    });
}

async Task CopyAsync(bool withWired)
{
    if (!room.InRoom)
    {
        Console.WriteLine($"Not in a room as far as the copier knows: room {room.RoomId}, floor plan {(room.Heightmap is null ? "not seen" : "seen")}");
        Tell("Enter a room first, then copy it.");
        return;
    }

    var wired = new Dictionary<long, RoomCopyWired>();
    IReadOnlyList<RoomCopyVariable>? variables = null;
    var note = "";
    if (withWired)
        (wired, variables, note) = await ReadWiredAsync();

    var copy = RoomRecorder.Record(room, hotel, ClassName, wired, variables);
    var name = string.Concat(copy.Name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Trim();
    var path = Path.Combine(directory, $"{name} ({copy.SourceRoomId}) {copy.CopiedAt:yyyyMMdd-HHmmss}.json");
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(copy, json));
    Console.WriteLine($"Copied {copy.Name}: {copy.FloorItems.Count} floor and {copy.WallItems.Count} wall items, {wired.Count} wired boxes to {path}");
    Tell($"Room copied: {copy.FloorItems.Count} floor and {copy.WallItems.Count} wall items{(withWired ? $", {wired.Count} wired boxes" : "")}{note}, saved as {Path.GetFileName(path)}");
}

// each wired box opened in turn, its dialog read instead of shown; then the room's variables, which the boxes use by
// ids the import must find again
async Task<(Dictionary<long, RoomCopyWired> Wired, IReadOnlyList<RoomCopyVariable>? Variables, string Note)> ReadWiredAsync()
{
    var read = new Dictionary<long, RoomCopyWired>();
    if (furniNames is null)
        return (read, null, " (no wired: the furnidata didn't load)");
    if (room.CanReadWired == false)
        return (read, null, " (no wired: this room doesn't let you read it)");
    if (!ext.Messages.TryGetHeader(new Identifier(ClientType.Flash, Direction.Out, "Open"), out var open))
        return (read, null, " (no wired: the client has no Open message)");

    var boxes = room.FloorItems
        .Where(item => ClassName(ItemType.Floor, item.Kind) is { } named && boxPrefixes.Any(named.StartsWith))
        .Select(item => (long)item.Id)
        .ToList();
    Tell($"Reading {boxes.Count} wired boxes, about {Math.Ceiling(boxes.Count * askEvery.TotalSeconds)} seconds...");

    var unanswered = 0;
    foreach (var id in boxes)
    {
        var answer = wiredReader.ExpectAsync(id, answerWithin);
        var body = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(body, (int)id);
        using (var packet = new Packet(open, ClientType.Flash, new PacketBuffer(body)))
            ext.Send(packet);
        if (await answer is { } settings)
            read[id] = settings;
        else
            unanswered++;
        await Task.Delay(askEvery);
    }
    var note = unanswered > 0 ? $" ({unanswered} didn't answer)" : "";

    var variables = await ReadVariablesAsync();
    if (variables is null && read.Values.Any(box => box.VariableIds.Count > 0))
        note += " (the variables didn't come, so the boxes lose theirs)";
    return (read, variables, note);
}

// every variable of the room, asked for as a client holding none of them
async Task<IReadOnlyList<RoomCopyVariable>?> ReadVariablesAsync()
{
    if (!ext.Messages.TryGetHeader(new Identifier(ClientType.Flash, Direction.Out, "WiredGetAllVariablesDiffs"), out var ask))
        return null;

    var answer = wiredReader.ExpectVariablesAsync(answerWithin);
    using (var packet = new Packet(ask, ClientType.Flash, new PacketBuffer(new byte[4]))) // none held
        ext.Send(packet);
    return await answer;
}

string? ClassName(ItemType type, int kind) => furniNames?.For(type, kind);

// a whisper only this client sees, from the player themselves
void Tell(string message)
{
    Console.WriteLine(message);
    if (room.SelfIndex is { } index)
        ext.Send(new AvatarWhisperMsg(message, index, 34));
}
