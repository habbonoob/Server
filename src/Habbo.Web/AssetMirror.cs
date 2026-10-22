using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Habbo.Web;

/// <summary>The asset mirror on disk (what Habbo.Tools' mirror command downloads), filled in as it is used: a file it
/// lacks from one of Habbo's image hosts is fetched there the first time it is asked for, kept, and served from disk
/// from then on, so the first request already gets it. What can't be fetched (a host it doesn't fetch from, or an
/// error) is written to wanted.txt for the mirror command; what the host doesn't have either is remembered for a while,
/// so a client retrying it costs one request upstream, not one each.</summary>
public sealed class AssetMirror
{
    private const string DirectoryKey = "Assets:MirrorDirectory";
    private const string DefaultDirectory = "habbo-assets";
    private const string FetchKey = "Assets:FetchMissing";
    private const string WantedFile = "wanted.txt";

    /// <summary>The named HttpClient it fetches with.</summary>
    public const string HttpClientName = "asset-mirror";

    /// <summary>Says plainly what it is: the server, asking for a file once to keep it.</summary>
    public const string UserAgent = "HotelAssetMirror/1.0";

    /// <summary>The hosts the mirror keeps, each a folder of its own.</summary>
    private static readonly HashSet<string> Hosts = ["images.habbo.com", "images-eussl.habbo.com", "www.habbo.com"];

    /// <summary>The hosts a missing file is fetched from: Habbo's image CDNs. The hotel's own site is mirrored only.</summary>
    private static readonly HashSet<string> FetchHosts = ["images.habbo.com", "images-eussl.habbo.com"];

    /// <summary>The largest file fetched; the client's biggest libraries are a few megabytes.</summary>
    private const long MaxFileBytes = 64L * 1024 * 1024;

    /// <summary>How long a file the host doesn't have is not asked for again.</summary>
    private static readonly TimeSpan MissingFor = TimeSpan.FromHours(1);

    /// <summary>The most files remembered as missing upstream; past it the expired ones go, and if none have, all of them,
    /// so made-up paths cannot fill memory.</summary>
    private const int MaxMissing = 10_000;

    /// <summary>The most files noted in wanted.txt; past it a file the mirror lacks is not noted, so made-up paths cannot
    /// fill memory or the disk.</summary>
    private const int MaxWanted = 10_000;

    /// <summary>How much it fetches in one run at most (in megabytes), so a client asking for file after file the CDN
    /// has cannot fill the disk; past it a file the mirror lacks is a 404 until the server restarts.</summary>
    private const string FetchLimitKey = "Assets:FetchLimitMegabytes";
    private const long DefaultFetchLimitMegabytes = 16 * 1024;

    private readonly IHttpClientFactory _http;
    private readonly ILogger<AssetMirror> _logger;
    private readonly bool _fetch;
    private readonly long _fetchLimitBytes;
    private long _fetchedBytes;
    private readonly ConcurrentDictionary<string, Lazy<Task<bool>>> _fetching = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _missing = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _wanted = new(StringComparer.Ordinal);
    private readonly Lock _wantedLock = new();

    public AssetMirror(IHttpClientFactory http, IConfiguration configuration, ILogger<AssetMirror> logger)
    {
        _http = http;
        _logger = logger;
        _fetch = configuration.GetValue(FetchKey, true);
        _fetchLimitBytes = configuration.GetValue(FetchLimitKey, DefaultFetchLimitMegabytes) * 1024 * 1024;
        var configured = configuration[DirectoryKey] ?? DefaultDirectory;
        Root = ResolveRoot(configured);
        if (Root is null)
            logger.LogWarning("No asset mirror at {Directory}: /mirror serves nothing until one is downloaded.", configured);
        else
        {
            logger.LogInformation("Serving the asset mirror in {Root} under /mirror{Fetching}.", Root,
                _fetch ? ", fetching what it lacks from Habbo's image hosts" : string.Empty);
            if (File.Exists(Path.Combine(Root, WantedFile)))
                foreach (var line in File.ReadLines(Path.Combine(Root, WantedFile)))
                    _wanted.TryAdd(line.Trim(), true);
        }
    }

    /// <summary>The mirror's folder; null when there is none.</summary>
    public string? Root { get; }

    /// <summary>The file for a host's path, fetched first if the mirror lacks it and may fetch it; null when there is
    /// none to serve.</summary>
    public async Task<string?> FileAsync(string host, string path, CancellationToken ct)
    {
        if (Root is null || !Hosts.Contains(host) || string.IsNullOrEmpty(path))
            return null;
        var hostRoot = Path.GetFullPath(Path.Combine(Root, host));
        var file = Path.GetFullPath(Path.Combine(hostRoot, path));
        // nothing outside the host's folder (no "..")
        if (!file.StartsWith(hostRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;
        if (File.Exists(file))
            return file;

        var url = $"https://{host}/{path}";
        if (!_fetch || !FetchHosts.Contains(host))
        {
            Want(url);
            return null;
        }
        if (_missing.TryGetValue(url, out var until) && until > DateTimeOffset.UtcNow)
            return null;
        if (Interlocked.Read(ref _fetchedBytes) >= _fetchLimitBytes)
            return null;

        // one download per file, however many ask for it at once; the request that started it may give up waiting
        // without stopping it, as the others still want it
        var fetch = _fetching.GetOrAdd(url, _ => new Lazy<Task<bool>>(() => FetchAsync(url, file)));
        try
        {
            return await fetch.Value.WaitAsync(ct) ? file : null;
        }
        finally
        {
            if (fetch.Value.IsCompleted)
                _fetching.TryRemove(url, out _);
        }
    }

    private async Task<bool> FetchAsync(string url, string file)
    {
        try
        {
            using var response = await _http.CreateClient(HttpClientName).GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Missing(url);
                return false;
            }
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > MaxFileBytes)
            {
                _logger.LogWarning("Fetching {Url} for the asset mirror got {Status}.", url, (int)response.StatusCode);
                Want(url);
                return false;
            }

            // written beside it and moved into place, so a half-written file is never served
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var partial = $"{file}.{Guid.NewGuid():N}.part";
            try
            {
                await using (var source = await response.Content.ReadAsStreamAsync())
                await using (var target = File.Create(partial))
                {
                    var buffer = new byte[81920];
                    long total = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer)) > 0)
                    {
                        total += read;
                        if (total > MaxFileBytes)
                            throw new InvalidDataException($"{url} is larger than {MaxFileBytes} bytes");
                        Interlocked.Add(ref _fetchedBytes, read);
                        await target.WriteAsync(buffer.AsMemory(0, read));
                    }
                }
                File.Move(partial, file, overwrite: true);
            }
            finally
            {
                File.Delete(partial);
            }

            _logger.LogInformation("Fetched {Url} into the asset mirror.", url);
            return true;
        }
        catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException or InvalidDataException)
        {
            _logger.LogWarning("Fetching {Url} for the asset mirror failed: {Error}", url, e.Message);
            Want(url);
            return false;
        }
    }

    /// <summary>Notes a file the mirror lacks, once, in wanted.txt, for the mirror command's "wanted" kind.</summary>
    private void Want(string url)
    {
        // nothing the mirror command would write outside its folder (a "..", however escaped)
        if (Root is null || _wanted.Count >= MaxWanted || Uri.UnescapeDataString(url).Split('/', '\\').Any(s => s is "." or ".."))
            return;
        if (!_wanted.TryAdd(url, true))
            return;
        _logger.LogInformation("The asset mirror has no {Url}: added to {Wanted}.", url, WantedFile);
        lock (_wantedLock)
            File.AppendAllLines(Path.Combine(Root, WantedFile), [url]);
    }

    /// <summary>Remembers a file the host doesn't have, for a while.</summary>
    private void Missing(string url)
    {
        if (_missing.Count >= MaxMissing)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (expired, _) in _missing.Where(m => m.Value <= now))
                _missing.TryRemove(expired, out _);
            if (_missing.Count >= MaxMissing)
                _missing.Clear();
        }
        _missing[url] = DateTimeOffset.UtcNow + MissingFor;
    }

    /// <summary>An absolute configured path as is, made if it doesn't exist yet (a deployment's empty volume); a
    /// relative one searched for from the binary's directory upwards (as the seed directory is), so it is found whether
    /// the host runs from bin/ or a published folder. Null when there is none.</summary>
    private static string? ResolveRoot(string configured)
    {
        if (Path.IsPathRooted(configured))
        {
            Directory.CreateDirectory(configured);
            return configured;
        }
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, configured);
            if (Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
