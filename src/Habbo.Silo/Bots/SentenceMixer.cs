namespace Habbo.Silo.Bots;

/// <summary>Makes up a sentence out of a bot's chat lines: it starts the way one of them starts, and each
/// word is followed by a word that follows it somewhere in them, until it reaches a word that ends one.</summary>
public static class SentenceMixer
{
    /// <summary>How many words a made-up sentence may run to, as a multiple of the longest line's.</summary>
    private const int MaxLengthFactor = 2;

    public static string Mix(IReadOnlyList<string> lines, Random random)
    {
        var sentences = lines.Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(words => words.Length > 0).ToList();
        if (sentences.Count == 0)
            return string.Empty;

        // null stands for the start and for the end of a line
        var next = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);
        var starts = new List<string>();
        foreach (var words in sentences)
        {
            starts.Add(words[0]);
            for (var i = 0; i < words.Length; i++)
            {
                if (!next.TryGetValue(words[i], out var followers))
                    next[words[i]] = followers = [];
                followers.Add(i + 1 < words.Length ? words[i + 1] : null);
            }
        }

        var limit = sentences.Max(words => words.Length) * MaxLengthFactor;
        var sentence = new List<string> { starts[random.Next(starts.Count)] };
        while (sentence.Count < limit)
        {
            var followers = next[sentence[^1]];
            if (followers[random.Next(followers.Count)] is not { } word)
                break;
            sentence.Add(word);
        }

        return string.Join(' ', sentence);
    }
}
