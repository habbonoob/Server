namespace Habbo.Camera;

/// <summary>Finds the camera's folders as the asset mirror is found: an absolute path as is, a relative one searched
/// for from the binary's directory upwards, so it is the same folder whether the host runs from bin/ or a published
/// folder.</summary>
internal static class CameraDirectories
{
    /// <summary>The asset mirror's folder setting, the one MirrorEndpoints serves.</summary>
    public const string MirrorKey = "Assets:MirrorDirectory";

    public const string DefaultMirror = "habbo-assets";

    /// <summary>The asset mirror's folder, when there is one.</summary>
    public static string? Mirror(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        Find(MirrorConfigured(configuration));

    public static string MirrorConfigured(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        configuration[MirrorKey] ?? DefaultMirror;

    public static string? Find(string configured)
    {
        if (Path.IsPathRooted(configured))
            return Directory.Exists(configured) ? configured : null;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, configured);
            if (Directory.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>A file, found as a folder is.</summary>
    public static string? FindFile(string configured)
    {
        if (Path.IsPathRooted(configured))
            return File.Exists(configured) ? configured : null;
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, configured);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }

    /// <summary>A folder to write to: the one there is, or a new one beside <paramref name="sibling"/> (the asset
    /// mirror), or beside the binary when that is not there either.</summary>
    public static string FindOrCreate(string configured, string sibling)
    {
        if (Find(configured) is { } found)
            return found;
        var parent = Path.IsPathRooted(configured) ? null
            : Find(sibling) is { } beside ? Path.GetDirectoryName(Path.GetFullPath(beside))
            : AppContext.BaseDirectory;
        var path = parent is null ? configured : Path.Combine(parent, configured);
        Directory.CreateDirectory(path);
        return path;
    }
}
