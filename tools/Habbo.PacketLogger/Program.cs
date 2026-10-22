using Habbo.PacketLogger;
using Xabbo;
using Xabbo.GEarth;
using Xabbo.Messages;

// A G-Earth extension that records every packet between the client and Habbo to disk and serves
// the recent ones over a local HTTP API, so a capture can be driven and read without copying it
// out of G-Earth's logger by hand. See README.md.

// --check <capture.txt> re-renders every packet of a G-Earth export and compares it with the
// original, to prove the .txt output stays readable by the tools that parse G-Earth's format.
if (args is ["--check", var checkPath])
{
    Environment.Exit(CheckCapture(checkPath));
}

// --encode <expression> prints the body an expression encodes to, without sending anything.
if (args is ["--encode", var encodeExpression])
{
    var parsed = PacketExpression.Parse(encodeExpression);
    Console.WriteLine($"{parsed.Direction} {parsed.Name ?? parsed.Id.ToString()} {Convert.ToHexString(parsed.Body)}");
    return;
}

var directory = Path.GetFullPath(Environment.GetEnvironmentVariable("PACKETLOG_DIR") ?? DefaultDirectory());
var port = int.TryParse(Environment.GetEnvironmentVariable("PACKETLOG_PORT"), out var p) ? p : 8790;
var buffer = int.TryParse(Environment.GetEnvironmentVariable("PACKETLOG_BUFFER"), out var b) ? b : 5000;
// /send wants this, so a web page (which can reach localhost too, but not this console) cannot send packets
var sendToken = Environment.GetEnvironmentVariable("PACKETLOG_TOKEN") is { Length: > 0 } t
    ? t
    : Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

using var log = new PacketLog(directory, buffer);
log.StartFlushing();

// Ctrl+C or closing the window ends the process without leaving the loop below, so `using` alone would
// never write what is still buffered.
Console.CancelKeyPress += (_, _) => log.Dispose();
AppDomain.CurrentDomain.ProcessExit += (_, _) => log.Dispose();

var ext = new GEarthExtension(new GEarthOptions
{
    Name = "Packet logger",
    Description = $"Logs packets to disk, API on localhost:{port}",
    Author = "habbo-emulator",
    Version = "1.0",
});

ext.Connected += e =>
{
    log.Client = e.Session.Client.Type;
    log.Mark($"connected: {e.Session.Hotel} {e.Session.Client.Identifier} {e.Session.Client.Version} ({e.Host}:{e.Port})");
    Console.WriteLine($"Connected to {e.Session.Hotel}. Logging to {log.CapturePath}.*");
};

ext.Disconnected += () =>
{
    log.Mark("disconnected");
    Console.WriteLine("Game disconnected.");
};

// The green play button pauses and resumes recording, for skipping the parts of a session that
// are only noise.
ext.Activated += () =>
{
    log.SetRecording(!log.Recording);
    Console.WriteLine(log.Recording ? "Recording resumed." : "Recording paused.");
};

ext.Intercepted += e =>
{
    var packet = e.Packet;
    var name = ext.Messages.TryGetNames(packet.Header, out MessageNames names)
        ? names.GetName(ext.Session.Client.Type)
        : null;
    var body = packet.Buffer.Span.ToArray();

    // Chat that is "/log" or starts with "/log " is a command to the logger, not something to say out loud;
    // "/logout" and the like are said as usual.
    if (e.Direction == Direction.Out && name is "Chat" or "Shout" or "Whisper"
        && PacketFormat.ReadLeadingString(body) is { } chat
        && (chat.Equals("/log", StringComparison.OrdinalIgnoreCase) || chat.StartsWith("/log ", StringComparison.OrdinalIgnoreCase)))
    {
        e.Block();
        HandleChatCommand(chat[4..].Trim());
        return;
    }

    log.Add(DateTimeOffset.Now, e.Direction, packet.Header.Value, name, body, e.IsBlocked);
};

var server = new ControlServer(log, () => new
{
    connected = ext.IsConnected,
    hotel = ext.IsConnected ? ext.Session.Hotel.ToString() : null,
    client = ext.IsConnected ? ext.Session.Client.Type.ToString() : null,
}, Send, sendToken, port);
server.Start();

Console.WriteLine($"Packet logger API on http://localhost:{port}/status");
Console.WriteLine($"/send is a POST with the header X-Token: {sendToken}");
Console.WriteLine($"Captures go to {Path.GetFullPath(directory)}");
Console.WriteLine("Add this extension in G-Earth (it connects on its own); play pauses/resumes recording.");

// Keep the API up when G-Earth is not running or restarts: reconnect instead of exiting, so the
// capture carries on in the same files.
while (true)
{
    try
    {
        await ext.RunAsync();
        Console.WriteLine("G-Earth closed the connection; reconnecting.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"G-Earth unavailable ({ex.Message.Split('\n')[0]}); retrying in 5s.");
    }
    log.Flush();
    await Task.Delay(TimeSpan.FromSeconds(5));
}

// Sends a packet to the server (out) or the client (in), resolving a message name through the
// names G-Earth handed over for this client.
object Send(PacketExpression expression)
{
    if (!ext.IsConnected)
        throw new InvalidOperationException("the game is not connected");
    var client = ext.Session.Client.Type;
    if (client == ClientType.Shockwave)
        throw new InvalidOperationException("sending is only supported on Flash and Unity");

    Header header;
    if (expression.Id is { } id)
        header = new Header(expression.Direction, id);
    else if (!ext.Messages.TryGetHeader(new Identifier(client, expression.Direction, expression.Name!), out header))
        throw new ArgumentException($"unknown {expression.Direction} message '{expression.Name}'");

    var name = ext.Messages.TryGetNames(header, out MessageNames names) ? names.GetName(client) : expression.Name;

    using var packet = new Packet(header, client, new PacketBuffer(expression.Body));
    ext.Send(packet);

    // Recorded as a marker too: whether G-Earth feeds an extension's own packets back through
    // the interceptor is up to G-Earth, and the capture should say what was injected either way.
    var direction = expression.Direction == Direction.Out ? "out" : "in";
    log.Mark($"sent {direction}:{name}[{header.Value}] {Convert.ToHexString(expression.Body)}");
    return new { dir = direction, id = header.Value, name, length = expression.Body.Length };
}

void HandleChatCommand(string command)
{
    var (verb, rest) = command.Split(' ', 2) switch
    {
        [var v, var r] => (v.ToLowerInvariant(), r.Trim()),
        [var v] => (v.ToLowerInvariant(), ""),
        _ => ("", ""),
    };

    switch (verb)
    {
        case "pause": log.SetRecording(false); break;
        case "resume": log.SetRecording(true); break;
        case "new": log.NewCapture(rest); break;
        // "/log <label>" and "/log mark <label>" both mark.
        case "mark": log.Mark(rest); break;
        case "": return;
        default: log.Mark(command); break;
    }
    Console.WriteLine($"/log {command}");
}

static int CheckCapture(string path)
{
    var line = new System.Text.RegularExpressions.Regex(@"^\s*(Incoming|Outgoing)\[(\d+)\]\s*->\s*(.*)$");
    var token = new System.Text.RegularExpressions.Regex(@"\[(\d+)\]|(.)", System.Text.RegularExpressions.RegexOptions.Singleline);
    int total = 0, failed = 0;

    foreach (var text in File.ReadLines(path))
    {
        var m = line.Match(text);
        if (!m.Success) continue;
        total++;

        var raw = token.Matches(m.Groups[3].Value)
            .Select(t => t.Groups[1].Success ? byte.Parse(t.Groups[1].Value) : (byte)t.Groups[2].Value[0])
            .ToArray();
        var packet = new LoggedPacket(0, default, m.Groups[1].Value == "Incoming" ? Direction.In : Direction.Out,
            short.Parse(m.Groups[2].Value), null, raw[6..], false);

        if (PacketFormat.Escape(PacketFormat.Frame(packet, ClientType.Flash)) != m.Groups[3].Value)
        {
            failed++;
            Console.WriteLine($"mismatch: {m.Groups[1].Value}[{m.Groups[2].Value}]");
        }
    }

    Console.WriteLine($"{total} packet(s), {failed} mismatch(es)");
    return failed == 0 ? 0 : 1;
}

// habbo-packetlogs/captures at the repository root, found from wherever `dotnet run` was started.
static string DefaultDirectory()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Habbo.slnx")))
            return Path.Combine(dir.FullName, "habbo-packetlogs", "captures");
    }
    return Path.Combine("habbo-packetlogs", "captures");
}
