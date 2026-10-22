using System.Text.Json;
using Habbo.Domain.Rooms.Copies;
using Habbo.Relay;
using Microsoft.Extensions.Options;

// Lets the web client (habbo-pixi) play on Habbo's own hotels, and records what it sends and receives:
//   /game?host=&port=   a WebSocket carried to that game server's TCP port (allowed hosts only)
//   POST /capture       the client's decoded packets, written as Habbo.PacketLogger writes captures
//   /capture/new?name=  starts a new pair of capture files
//   /mark?label=        writes a section header into the capture
//   /status             where the capture is, how much is in it
//   POST /roomcopy      a room the client's room copier copied, saved where the G-Earth copier saves its own
// Localhost only. See README.md.
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<RelayOptions>(builder.Configuration.GetSection(RelayOptions.Section));
builder.Services.AddSingleton<GameRelay>();
builder.Services.AddSingleton<CaptureLog>();
builder.Services.AddSingleton<RoomCopies>();
var options = builder.Configuration.GetSection(RelayOptions.Section).Get<RelayOptions>() ?? new RelayOptions();
builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(options.Port));
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.WithOrigins(options.AllowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

// only the client's own pages: a page elsewhere in the browser could otherwise open game connections through here
bool FromClient(HttpContext context) =>
    context.Request.Headers.Origin is { Count: > 0 } origin
        ? context.RequestServices.GetRequiredService<IOptions<RelayOptions>>().Value.AllowedOrigins.Contains(origin.ToString())
        : true;

app.Map("/game", async (HttpContext context, GameRelay relay) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
        return Results.BadRequest("a WebSocket upgrade");
    if (!FromClient(context))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    var host = context.Request.Query["host"].ToString();
    if (!int.TryParse(context.Request.Query["port"], out var port) || port is <= 0 or > 65535 || !relay.IsAllowed(host))
        return Results.BadRequest($"not a game server the relay carries to: {host}:{context.Request.Query["port"]}");
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await relay.RunAsync(socket, host, port, context.RequestAborted);
    return Results.Empty;
});

app.MapPost("/capture", async (HttpContext context, CaptureLog log) =>
{
    if (!FromClient(context))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    var packets = await context.Request.ReadFromJsonAsync<List<CapturedPacket>>();
    return Results.Ok(new { added = packets is null ? 0 : log.Add(packets) });
});

app.MapGet("/capture/new", (string? name, CaptureLog log) => Results.Ok(new { capture = log.NewCapture(name) }));
app.MapGet("/mark", (string label, CaptureLog log) =>
{
    log.Mark(label);
    return Results.Ok(new { marked = label });
});
app.MapGet("/status", (CaptureLog log) => Results.Ok(log.Status()));

app.MapPost("/roomcopy", async (HttpContext context, RoomCopies copies) =>
{
    if (!FromClient(context))
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    RoomCopy? copy;
    try
    {
        copy = await context.Request.ReadFromJsonAsync<RoomCopy>(context.RequestAborted);
    }
    catch (JsonException ex)
    {
        return Results.BadRequest($"not a room copy: {ex.Message}");
    }
    if (copy is null || string.IsNullOrEmpty(copy.Heightmap) || copy.FloorItems is null || copy.WallItems is null)
        return Results.BadRequest("not a room copy: no floor plan or furni");
    var path = await copies.SaveAsync(copy, context.RequestAborted);
    app.Logger.LogInformation("Room copied: {Name} ({FloorItems} floor and {WallItems} wall items) to {Path}", copy.Name,
        copy.FloorItems.Count, copy.WallItems.Count, path);
    return Results.Ok(new { path, file = Path.GetFileName(path) });
});

app.Logger.LogInformation("Relay on http://localhost:{Port}: start the client with ?relay={RelayPort}&capture={CapturePort}.", options.Port, options.Port, options.Port);
app.Run();
