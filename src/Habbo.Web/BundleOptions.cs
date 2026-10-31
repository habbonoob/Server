namespace Habbo.Web;

/// <summary>Where the client's own libraries are, for <see cref="AssetBundles"/> (the "Bundles" section).</summary>
public sealed class BundleOptions
{
    public const string Section = "Bundles";

    /// <summary>The release's decompiled source: its component classes (<c>HabboRoomUICom.as</c>, …) and the
    /// <c>_assets</c> they embed.</summary>
    public string? ClientSourceDirectory { get; set; }

    /// <summary>The release's <c>local_include</c> libraries, as .hab.</summary>
    public string? LocalIncludeDirectory { get; set; }
}
