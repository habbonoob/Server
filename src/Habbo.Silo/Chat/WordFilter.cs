using System.Text.RegularExpressions;
using Habbo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Habbo.Silo.Chat;

/// <summary>The hotel's word filter: what players write in chat, names and mottos is checked
/// against the filtered words (and a room's own), whole words or inside others, ignoring case.</summary>
public sealed class WordFilter(IDbContextFactory<AppDbContext> dbFactory, IOptions<ChatOptions> options) : Habbo.Silo.Abstractions.Hotel.IReloadable
{
    public string ReloadName => "wordfilter";

    public Task ReloadAsync()
    {
        Reload();
        return Task.CompletedTask;
    }

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    /// <summary>The words as last read, and the reload they were read after.</summary>
    private sealed record LoadedWords(int Version, IReadOnlyDictionary<string, string> Words);

    private LoadedWords? _loaded;

    /// <summary>Counts reloads: words read before the latest one are not used again.</summary>
    private int _version;

    /// <summary>The text with every filtered word replaced, and the first word it found (null when clean).</summary>
    public async Task<(string Text, string? FirstHit)> FilterAsync(string text, IEnumerable<string>? roomWords = null)
    {
        var words = await WordsAsync();
        string? first = null;
        foreach (var (word, replacement) in words.Concat((roomWords ?? []).Select(w => new KeyValuePair<string, string>(w, string.Empty))))
        {
            if (word.Length == 0 || text.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            first ??= word;
            // the replacement as written: "$0" in one is text, not the word put back
            var with = replacement.Length > 0 ? replacement : options.Value.FilterReplacement;
            text = Regex.Replace(text, Regex.Escape(word), _ => with, RegexOptions.IgnoreCase);
        }

        return (text, first);
    }

    /// <summary>Whether text holds a filtered word.</summary>
    public async Task<bool> IsCleanAsync(string text) => (await FilterAsync(text)).FirstHit is null;

    /// <summary>Forgets the words it knows, after the hotel changed them; what a load already under way
    /// reads is not used again either.</summary>
    public void Reload() => Interlocked.Increment(ref _version);

    private async Task<IReadOnlyDictionary<string, string>> WordsAsync()
    {
        if (Current() is { } words)
            return words;

        await _loadLock.WaitAsync();
        try
        {
            if (Current() is { } loaded)
                return loaded;

            var version = Volatile.Read(ref _version);
            await using var db = await dbFactory.CreateDbContextAsync();
            var read = await db.FilteredWords.AsNoTracking().ToDictionaryAsync(w => w.Word, w => w.Replacement);
            _loaded = new LoadedWords(version, read);
            return read;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <summary>The words read since the latest reload, if any were.</summary>
    private IReadOnlyDictionary<string, string>? Current() =>
        _loaded is { } loaded && loaded.Version == Volatile.Read(ref _version) ? loaded.Words : null;
}
