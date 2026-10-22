namespace Habbo.Tools.AssetMirror;

/// <summary>external_variables: key=value lines, where a value may name others as ${key}.</summary>
public sealed class ClientVariables
{
    private const int MaxDepth = 10;

    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public ClientVariables(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.StartsWith('#'))
                continue;
            _values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }
    }

    /// <summary>Gives a variable the value the client itself would, when the file does not set it: url.prefix, say,
    /// which the client takes from the hotel it runs on.</summary>
    public void SetDefault(string key, string value) => _values.TryAdd(key, value);

    /// <summary>The value with every ${key} in it filled in; the fallback when there is no such key.</summary>
    public string Get(string key, string fallback = "") =>
        _values.TryGetValue(key, out var value) ? Expand(value, 0) : fallback;

    /// <summary>A url the client would take as it is: protocol-relative and plain http ones made https.</summary>
    public string Url(string key, string fallback = "")
    {
        var url = Get(key, fallback);
        return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url
            : url.StartsWith("http://", StringComparison.Ordinal) ? "https://" + url["http://".Length..]
            : url;
    }

    /// <summary>Every variable, its value filled in.</summary>
    public IEnumerable<KeyValuePair<string, string>> All() =>
        _values.Keys.Select(key => new KeyValuePair<string, string>(key, Get(key)));

    /// <summary>A text with every ${key} in it filled in from the variables.</summary>
    public string Expand(string text) => Expand(text, 0);

    private string Expand(string value, int depth)
    {
        if (depth > MaxDepth)
            return value;

        var start = value.IndexOf("${", StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = value.IndexOf('}', start);
            if (end < 0)
                break;
            var key = value[(start + 2)..end];
            var replacement = _values.TryGetValue(key, out var found) ? Expand(found, depth + 1) : string.Empty;
            value = value[..start] + replacement + value[(end + 1)..];
            start = value.IndexOf("${", start + replacement.Length, StringComparison.Ordinal);
        }

        return value;
    }
}
