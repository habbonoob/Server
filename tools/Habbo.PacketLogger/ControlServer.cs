using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xabbo;

namespace Habbo.PacketLogger;

/// <summary>
/// A small HTTP API on localhost for driving the logger from a script or a terminal. Every route
/// but /send answers GET, so plain <c>curl</c> works; /send is a POST with the run's token in
/// X-Token. A request from a web page (one with an Origin) is refused, so a page open in the
/// browser cannot drive it. See the README for the routes.
/// </summary>
public sealed class ControlServer(PacketLog log, Func<object> connection, Func<PacketExpression, object> send, string sendToken, int port)
{
    const string TokenHeader = "X-Token";

    static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>How long a caller's name or exclude pattern may take on one packet name before it counts as no match.</summary>
    static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(50);

    HttpListener _listener = Listener(port, withAddress: true);

    /// <summary>
    /// Listens on localhost, and on 127.0.0.1 too when Windows allows it: HTTP.sys lets anyone take a localhost
    /// prefix but wants a URL ACL (or admin) for 127.0.0.1, so without one the API is reached as localhost only.
    /// </summary>
    public void Start()
    {
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException)
        {
            _listener.Close();
            _listener = Listener(port, withAddress: false);
            _listener.Start();
            Console.WriteLine($"API on localhost only (127.0.0.1:{port} needs a URL ACL on Windows).");
        }
        _ = Task.Run(AcceptLoop);
    }

    static HttpListener Listener(int port, bool withAddress)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        if (withAddress)
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        return listener;
    }

    async Task AcceptLoop()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try { context = await _listener.GetContextAsync(); }
            catch (Exception) when (!_listener.IsListening) { return; }

            _ = Task.Run(() => Handle(context));
        }
    }

    async Task Handle(HttpListenerContext context)
    {
        var response = context.Response;
        int status = 200;
        object body;
        try
        {
            // no browser page is a caller: scripts and curl send no Origin
            if (context.Request.Headers["Origin"] is not null)
                throw new UnauthorizedAccessException("requests from web pages are refused");
            body = await Route(context.Request);
        }
        catch (UnauthorizedAccessException ex)
        {
            status = 403;
            body = new { error = ex.Message };
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or InvalidOperationException)
        {
            status = 400;
            body = new { error = ex.Message };
        }
        catch (Exception ex)
        {
            status = 500;
            body = new { error = ex.Message };
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, Json) + "\n");
            response.StatusCode = status;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
        }
        finally
        {
            response.Close();
        }
    }

    async Task<object> Route(HttpListenerRequest request)
    {
        var q = request.QueryString;
        switch (request.Url!.AbsolutePath.TrimEnd('/').ToLowerInvariant())
        {
            case "":
            case "/status":
                return new { connection = connection(), log = log.Status() };

            case "/mark":
                var label = q["label"] ?? throw new ArgumentException("label is required");
                log.Mark(label);
                return new { marked = label };

            case "/capture":
                return new { capture = log.NewCapture(q["name"]) };

            case "/pause":
                log.SetRecording(false);
                return log.Status();

            case "/resume":
                log.SetRecording(true);
                return log.Status();

            case "/flush":
                log.Flush();
                return log.Status();

            case "/recent":
            {
                // without since: the latest packets; with it: the next ones after it, oldest first, so paging on with
                // since=lastSeq misses none
                var filter = Filter(q);
                var packets = log.Recent(Long(q["since"], 0), Int(q["limit"], 100), filter, newest: q["since"] is null);
                return Page(packets, q);
            }

            case "/wait":
                return await Wait(Long(q["since"], log.LastSeq), Filter(q), q);

            case "/send":
            {
                // The expression comes from ?e= or the request body, which saves quoting it for a URL:
                //   curl localhost:8790/send -H 'X-Token: <token>' --data-binary '{out:MoveAvatar}{i:5}{i:7}'
                if (request.HttpMethod != "POST")
                    throw new ArgumentException("/send is a POST");
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(request.Headers[TokenHeader] ?? ""), Encoding.UTF8.GetBytes(sendToken)))
                    throw new UnauthorizedAccessException($"/send wants the token printed at startup in {TokenHeader}");
                var text = q["e"];
                if (string.IsNullOrWhiteSpace(text) && request.HasEntityBody)
                {
                    using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                    text = await reader.ReadToEndAsync();
                }
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("pass the packet expression as ?e= or the request body");

                // Everything the send causes arrives after this, so it is where a wait starts.
                var since = log.LastSeq;
                var sent = send(PacketExpression.Parse(text));

                // ?wait=<name regex> sends and waits for the answer in one call.
                if (string.IsNullOrEmpty(q["wait"]))
                    return new { sent, since };
                var filter = Filter(q, nameKey: "wait");
                return new { sent, since, reply = await Wait(since, filter, q) };
            }

            default:
                throw new ArgumentException(
                    "unknown route; try /status /recent /wait /send /mark /capture /pause /resume /flush");
        }
    }

    async Task<object> Wait(long since, Func<LoggedPacket, bool> filter,
        System.Collections.Specialized.NameValueCollection q)
    {
        // Long-polls until a packet after `since` matches, so a caller can do something in the
        // client and then wait for the server's answer instead of guessing a delay. It wakes when a
        // packet arrives rather than polling, and looks only at what came after `since`; the first
        // matches are returned, so the first answer is the one seen.
        var deadline = DateTime.UtcNow.AddSeconds(Math.Clamp(Int(q["timeout"], 30), 1, 300));
        while (true)
        {
            var next = log.NextPacket;
            var packets = log.Recent(since, Int(q["limit"], 100), filter);
            if (packets.Count > 0) return Page(packets, q);

            var left = deadline - DateTime.UtcNow;
            if (left <= TimeSpan.Zero || await Task.WhenAny(next, Task.Delay(left)) != next)
                return new { timedOut = true, since };
        }
    }

    object Page(List<LoggedPacket> packets, System.Collections.Specialized.NameValueCollection q) => new
    {
        lastSeq = packets.Count > 0 ? packets[^1].Seq : (long?)null,
        count = packets.Count,
        packets = packets.Select(p => log.ToJson(p, includeText: q["text"] is "1" or "true")),
    };

    static Func<LoggedPacket, bool> Filter(System.Collections.Specialized.NameValueCollection q, string nameKey = "name")
    {
        Direction? direction = q["dir"]?.ToLowerInvariant() switch
        {
            null or "" => null,
            "in" => Direction.In,
            "out" => Direction.Out,
            _ => throw new ArgumentException("dir must be in or out"),
        };
        var name = Pattern(q[nameKey]);
        var exclude = Pattern(q["exclude"]);
        short? id = short.TryParse(q["id"], out var i) ? i : null;

        return p => (direction is null || p.Direction == direction)
                    && (id is null || p.Id == id)
                    && (name is null || Matches(name, p.Name))
                    && (exclude is null || !Matches(exclude, p.Name));
    }

    static Regex? Pattern(string? pattern) => string.IsNullOrEmpty(pattern)
        ? null
        : new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, PatternTimeout);

    /// <summary>Whether the name matches; a pattern that runs out of time does not.</summary>
    static bool Matches(Regex pattern, string? name)
    {
        try
        {
            return pattern.IsMatch(name ?? "");
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    static int Int(string? value, int fallback) => int.TryParse(value, out var v) ? v : fallback;
    static long Long(string? value, long fallback) => long.TryParse(value, out var v) ? v : fallback;
}
