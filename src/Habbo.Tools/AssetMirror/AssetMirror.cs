using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Habbo.Tools.AssetMirror;

/// <summary>Downloads into the mirror: each url under its host and path, skipping what is there already, retrying what
/// fails for a moment, and keeping a list of what could not be had. The gamedata comes first, as everything else is
/// found from it; then every kind of asset downloads at once, sharing the same number of downloads at a time, each with
/// a progress bar of its own. Requests look like a browser's, as the hotel's DDoS protection turns anything else away.</summary>
public sealed class AssetMirror : IDisposable
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0 Safari/537.36";

    private const int Attempts = 3;
    /// <summary>What went wrong fetching (errors, timeouts, refusals), and apart from it what the hotel simply does
    /// not have (404), so the one is not lost among the other.</summary>
    private const string FailedFile = "failed.txt";
    private const string MissingFile = "missing.txt";

    /// <summary>The urls the server's /mirror was asked for and did not have (MirrorEndpoints writes it).</summary>
    private const string WantedFile = "wanted.txt";

    /// <summary>Where the gamedata files go, each under its own name rather than its url's hash.</summary>
    private const string GamedataFolder = "gamedata";
    private const string HabbopagesFolder = "habbopages";

    /// <summary>The hotel's own address, which the client fills in itself where the variables leave it out.</summary>
    private const string UrlPrefix = "url.prefix";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    private readonly MirrorOptions _options;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _slots;
    private readonly ConcurrentBag<string> _failed = [];
    private readonly ConcurrentBag<string> _missing = [];

    public AssetMirror(MirrorOptions options)
    {
        _options = options;
        _slots = new SemaphoreSlim(options.Workers);
        _http = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            MaxConnectionsPerServer = options.Workers,
        })
        {
            Timeout = TimeSpan.FromSeconds(60),
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        _http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        _http.DefaultRequestHeaders.Referrer = new Uri(options.Site + "/");
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var files = await GamedataAsync(ct);
        var variables = new ClientVariables(Encoding.UTF8.GetString(files.GetValueOrDefault("external_variables") ?? []));
        variables.SetDefault(UrlPrefix, _options.Site);
        var client = variables.Url("flash.client.url");

        var board = new ProgressBoard();
        var work = new List<Task>();
        foreach (var kind in _options.Kinds.Where(k => k != AssetKind.Gamedata).Order())
        {
            var bar = board.Add(kind.ToString().ToLowerInvariant());
            work.Add(RunKindAsync(kind, bar, files, variables, client, ct));
        }

        board.Start();
        await using (board)
        {
            await Task.WhenAll(work);
        }

        await File.WriteAllLinesAsync(Path.Combine(_options.Out, FailedFile), _failed.Order(StringComparer.Ordinal), ct);
        await File.WriteAllLinesAsync(Path.Combine(_options.Out, MissingFile), _missing.Order(StringComparer.Ordinal), ct);
        Console.WriteLine($"done: {_failed.Count} failed ({FailedFile}), {_missing.Count} not on the hotel ({MissingFile})");
    }

    /// <summary>One kind of asset, start to finish; its bar says so when it is done.</summary>
    private async Task RunKindAsync(AssetKind kind, ProgressBar bar, IReadOnlyDictionary<string, byte[]> files,
        ClientVariables variables, string client, CancellationToken ct)
    {
        try
        {
            switch (kind)
            {
                case AssetKind.Furni when files.TryGetValue("furnidata", out var furnidata):
                    await FetchAllAsync(bar, AssetUrls.Furni(furnidata, variables), ct);
                    break;
                case AssetKind.Figure when client.Length > 0:
                    await FetchAllAsync(bar, AssetUrls.AvatarClient(client), ct);
                    await LibrariesAsync(bar, client + "figuremap.xml", "lib", "id",
                        variables.Get("flash.dynamic.avatar.download.name.template", "%libname%.swf"), ct);
                    break;
                case AssetKind.Effects when client.Length > 0:
                    await LibrariesAsync(bar, client + "effectmap.xml", "effect", "lib", "%libname%.swf", ct);
                    break;
                case AssetKind.Pets when client.Length > 0:
                    await FetchAllAsync(bar, AssetUrls.Pets(client, variables), ct);
                    break;
                case AssetKind.Badges when files.TryGetValue("external_texts", out var texts):
                    await BadgesAsync(bar, Encoding.UTF8.GetString(texts), variables, ct);
                    break;
                case AssetKind.Sounds:
                    await ProbeAllAsync(bar, AssetUrls.Sounds(variables, _options.MaxSample), ct);
                    break;
                case AssetKind.Images:
                    await FetchAllAsync(bar, AssetUrls.ImageReferences(
                        files.Values.Select(data => Encoding.UTF8.GetString(data)), variables)
                        .Concat(AssetUrls.RelativeImages(variables)).Distinct(), ct);
                    break;
                case AssetKind.Habbicons:
                    await HabbiconsAsync(bar, variables, ct);
                    break;
                case AssetKind.Wanted:
                    await WantedAsync(bar, ct);
                    break;
                case AssetKind.Habbopages:
                    await HabbopagesAsync(bar, files.Values.Select(data => Encoding.UTF8.GetString(data)), variables, ct);
                    break;
                case AssetKind.Catalogue:
                    await ProbeAllAsync(bar, AssetUrls.CatalogueIcons(variables, _options.MaxIcon), ct);
                    break;
                case AssetKind.BadgeParts when await BadgePartNamesAsync(ct) is { Count: > 0 } names:
                    await FetchAllAsync(bar, AssetUrls.BadgeParts(names, variables), ct);
                    break;
                case AssetKind.BadgeParts:
                    bar.Note = $"no badge parts to go on: {_options.BadgePartsFile} is missing or empty";
                    break;
                default:
                    bar.Note = "nothing to go on: the gamedata it needs did not come";
                    break;
            }
        }
        finally
        {
            bar.Finished = true;
        }
    }

    /// <summary>The hashes manifest, kept beside the mirror, and the gamedata files it lists (and the ones served
    /// beside them), by name.</summary>
    private async Task<Dictionary<string, byte[]>> GamedataAsync(CancellationToken ct)
    {
        var json = await _http.GetByteArrayAsync($"{_options.Site}/gamedata/hashes", ct);
        var manifest = JsonSerializer.Deserialize<GamedataManifest>(json, Json)
                       ?? throw new InvalidOperationException("The gamedata manifest was empty.");
        Directory.CreateDirectory(_options.Out);
        await File.WriteAllBytesAsync(Path.Combine(_options.Out, $"hashes.{_options.Hotel}.json"), json, ct);

        var hashes = manifest.Hashes.ToDictionary(h => h.Name, h => h.Hash);
        var entries = manifest.Hashes.Concat(GamedataExtra.All
            .Where(extra => hashes.ContainsKey(extra.BaseName))
            .Select(extra => new GamedataHash(extra.FileName,
                $"{_options.Site}/gamedata/{extra.Path}", hashes[extra.BaseName]) { SaveAs = extra.FileName }))
            .ToList();

        // what the last run fetched: a file whose hash has not changed since is not fetched again
        var statePath = Path.Combine(_options.Out, $"gamedata.{_options.Hotel}.json");
        var previous = File.Exists(statePath)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllBytesAsync(statePath, ct)) ?? []
            : [];

        var fetched = await Task.WhenAll(entries.Select(async entry =>
        {
            var path = Path.Combine(_options.Out, new Uri(entry.Url).Host, GamedataFolder, entry.FileName);
            if (previous.GetValueOrDefault(entry.FileName) != entry.Hash && File.Exists(path))
                File.Delete(path);
            return (entry.Name, entry.FileName, entry.Hash, Data: await FetchAsync(entry.FileUrl, bar: null, listMissing: true, ct, path));
        }));
        await File.WriteAllBytesAsync(statePath, JsonSerializer.SerializeToUtf8Bytes(
            fetched.Where(f => f.Data is not null).ToDictionary(f => f.FileName, f => f.Hash), Json), ct);
        foreach (var (_, fileName, _, data) in fetched)
            Console.WriteLine(data is null ? $"gamedata: {fileName} could not be had" : $"gamedata: {fileName} ({data.Length.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} bytes)");
        return fetched.Where(f => f.Data is not null).ToDictionary(f => f.Name, f => f.Data!);
    }

    /// <summary>The help pages: every one the gamedata names, then every one those link to, and so on, each kept as
    /// &lt;host&gt;/gamedata/habbopages/&lt;name&gt;.txt (a page and a folder may share a name), and the images they show.</summary>
    private async Task HabbopagesAsync(ProgressBar bar, IEnumerable<string> texts, ClientVariables variables, CancellationToken ct)
    {
        var baseUrl = variables.Url("habbopages.url", $"{_options.Site}/gamedata/habbopages/");
        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";
        var seen = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        var images = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);
        var level = texts.SelectMany(AssetUrls.HabbopageNames).Concat(_options.Pages)
            .Where(name => seen.TryAdd(name, true)).ToList();

        while (level.Count > 0)
        {
            if (_options.Limit > 0)
                level = level.Take(_options.Limit).ToList();
            bar.AddToTotal(level.Count);

            var next = new ConcurrentBag<string>();
            await Parallel.ForEachAsync(level, new ParallelOptions { MaxDegreeOfParallelism = _options.Workers, CancellationToken = ct },
                async (name, token) =>
                {
                    var url = baseUrl + name;
                    var path = Path.Combine([_options.Out, new Uri(url).Host, GamedataFolder, HabbopagesFolder, .. (name + ".txt").Split('/')]);
                    if (await FetchAsync(url, bar, listMissing: true, token, path) is not { } data)
                        return;

                    var page = Encoding.UTF8.GetString(data);
                    foreach (var linked in AssetUrls.HabbopageNames(page).Where(linked => seen.TryAdd(linked, true)))
                        next.Add(linked);
                    foreach (var image in AssetUrls.PageImages(page, new Uri(url)))
                        images.TryAdd(image, true);
                });

            // a quick try looks at the pages the gamedata names, not the whole web of them
            level = _options.Limit > 0 ? [] : [.. next];
        }

        if (!images.IsEmpty)
            await FetchAllAsync(bar, images.Keys, ct);
    }

    /// <summary>The habbicons: index and sheets, then the animation of every habbicon the index lists (probed: most
    /// have none).</summary>
    private async Task HabbiconsAsync(ProgressBar bar, ClientVariables variables, CancellationToken ct)
    {
        var root = variables.Url("habbicons.asset.root", "https://images.habbo.com/habbicons");
        var hash = variables.Get("habbicons.hash");
        if (hash.Length == 0)
        {
            bar.Note = "no habbicons.hash in the variables";
            return;
        }
        await FetchAllAsync(bar, AssetUrls.Habbicons(root, hash), ct);
        var index = Path.Combine([_options.Out, new Uri(root).Host, .. new Uri(root).AbsolutePath.Trim('/').Split('/'), hash, "habbicons.json"]);
        if (!File.Exists(index))
            return;
        using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(index, ct));
        var ids = json.RootElement.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
            .SelectMany(p => p.Value.EnumerateArray())
            .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number)
            .Select(e => e.GetProperty("id").GetInt32());
        await ProbeAllAsync(bar, AssetUrls.HabbiconAnimations(root, hash, ids), ct);
    }

    /// <summary>What the server asked the mirror for and did not find (wanted.txt, one url a line).</summary>
    private async Task WantedAsync(ProgressBar bar, CancellationToken ct)
    {
        var path = Path.Combine(_options.Out, WantedFile);
        if (!File.Exists(path))
        {
            bar.Note = $"nothing wanted: {WantedFile} is not there yet";
            return;
        }
        var urls = (await File.ReadAllLinesAsync(path, ct))
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("https://", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        await FetchAllAsync(bar, urls, ct);
    }

    private async Task LibrariesAsync(ProgressBar bar, string mapUrl, string element, string attribute, string template, CancellationToken ct)
    {
        if (await FetchAsync(mapUrl, bar: null, listMissing: true, ct) is { } map)
            await FetchAllAsync(bar, AssetUrls.Libraries(mapUrl, map, element, attribute, template), ct);
        else
            bar.Note = $"{mapUrl} could not be had";
    }

    /// <summary>Badges are mostly gifs; one that is not is tried as a png, which the bar then counts too.</summary>
    private async Task BadgesAsync(ProgressBar bar, string texts, ClientVariables variables, CancellationToken ct)
    {
        var missing = new ConcurrentBag<string>();
        await ForEachAsync(bar, AssetUrls.Badges(texts, variables), async (url, token) =>
        {
            if (await FetchAsync(url, bar, listMissing: false, token) is null)
                missing.Add(url[..^".gif".Length] + ".png");
        }, ct);

        if (!missing.IsEmpty)
            await FetchAllAsync(bar, missing, ct);
    }

    private Task FetchAllAsync(ProgressBar bar, IEnumerable<string> urls, CancellationToken ct) =>
        ForEachAsync(bar, urls, async (url, token) => await FetchAsync(url, bar, listMissing: true, token), ct);

    /// <summary>Tries numbered files, where a gap is no failure: there simply is no such number.</summary>
    private Task ProbeAllAsync(ProgressBar bar, IEnumerable<string> urls, CancellationToken ct) =>
        ForEachAsync(bar, urls, async (url, token) => await FetchAsync(url, bar, listMissing: false, token), ct);

    /// <summary>The badge part file names the hotel's seed gives (fileName and maskFileName of each part).</summary>
    private async Task<List<string>> BadgePartNamesAsync(CancellationToken ct)
    {
        if (!File.Exists(_options.BadgePartsFile))
            return [];

        await using var stream = File.OpenRead(_options.BadgePartsFile);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!document.RootElement.TryGetProperty("parts", out var parts))
            return [];

        return parts.EnumerateArray()
            .SelectMany(part => new[] { "fileName", "maskFileName" }.Select(key =>
                part.TryGetProperty(key, out var value) ? value.GetString() ?? string.Empty : string.Empty))
            .Where(name => name.Length > 0)
            .ToList();
    }

    private async Task ForEachAsync(ProgressBar bar, IEnumerable<string> urls, Func<string, CancellationToken, ValueTask> fetch, CancellationToken ct)
    {
        var list = urls.Distinct().ToList();
        if (_options.Limit > 0)
            list = list.Take(_options.Limit).ToList();
        bar.AddToTotal(list.Count);

        // each kind may use every slot; the slots themselves keep the total down
        await Parallel.ForEachAsync(list, new ParallelOptions { MaxDegreeOfParallelism = _options.Workers, CancellationToken = ct }, fetch);
    }

    /// <summary>The file's bytes, from the mirror when it is there already, else downloaded into it; null when it could
    /// not be had. A file the hotel does not have goes on the missing list, unless it was only being tried (a numbered
    /// file, a badge's gif before its png); anything else that goes wrong goes on the failed list.</summary>
    private async Task<byte[]?> FetchAsync(string url, ProgressBar? bar, bool listMissing, CancellationToken ct, string? path = null)
    {
        path ??= PathOf(url);
        if (path is null)
        {
            _failed.Add($"{url}\tits path leads outside the mirror");
            bar?.Failure();
            return null;
        }
        if (!_options.Force && File.Exists(path) && new FileInfo(path).Length > 0)
        {
            bar?.AlreadyThere();
            return await File.ReadAllBytesAsync(path, ct);
        }

        for (var attempt = 1; ; attempt++)
        {
            await _slots.WaitAsync(ct);
            try
            {
                using var response = await _http.GetAsync(url, ct);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    if (listMissing)
                        _missing.Add(url);
                    bar?.NotThere(listMissing);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync(ct);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, data, ct);
                bar?.Fetched(data.Length);
                return data;
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                if (attempt >= Attempts)
                {
                    _failed.Add($"{url}\t{error.Message}");
                    bar?.Failure();
                    return null;
                }
            }
            finally
            {
                _slots.Release();
            }

            await Task.Delay(TimeSpan.FromSeconds(1 << (attempt - 1)), ct);
        }
    }

    /// <summary>Where a url goes in the mirror: its host, then its path. Null when that would be outside the mirror (a
    /// "..", or a separator, escaped in a segment).</summary>
    private string? PathOf(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToList();
        if (segments.Any(s => s is "." or ".." || s.IndexOfAny(['/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0))
            return null;
        var root = Path.GetFullPath(_options.Out);
        var path = Path.GetFullPath(Path.Combine([root, uri.Host, .. segments]));
        return path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? path : null;
    }

    public void Dispose()
    {
        _http.Dispose();
        _slots.Dispose();
    }
}
