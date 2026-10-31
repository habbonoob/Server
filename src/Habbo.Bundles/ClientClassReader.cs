using System.Text.RegularExpressions;

namespace Habbo.Bundles;

/// <summary>Reads what one class of the client binary embeds, with no manifest to go by: the loading screen's photo
/// splash (<c>splash/PhotoSplashScreen</c>) and the like. Each <c>[Embed]</c> is named after its member; images
/// are images, <c>.bin</c>/<c>.xml</c>/<c>.txt</c> files text documents, anything else (fonts, sounds) left out.
/// Given <paramref name="members"/>, only those: the splash shows one of its thirty photos.</summary>
public static partial class ClientClassReader
{
    public static BundleLibrary Read(string sourceDirectory, string classPath, IReadOnlySet<string>? members = null)
    {
        if (!ClassPath().IsMatch(classPath))
            throw new ArgumentException($"{classPath} is not a class path.", nameof(classPath));
        var classFile = Path.Combine(sourceDirectory, classPath + ".as");
        if (!File.Exists(classFile))
            throw new FileNotFoundException($"The client has no class {classPath}.");

        var library = new BundleLibrary(classPath.Replace('/', '.'));
        foreach (Match embed in ClientComponentReader.EmbedPattern().Matches(File.ReadAllText(classFile)))
        {
            var member = embed.Groups["member"].Value;
            if (members is not null && !members.Contains(member))
                continue;
            var file = Path.Combine(sourceDirectory, embed.Groups["file"].Value.TrimStart('/'));
            if (!File.Exists(file))
                continue;
            switch (Path.GetExtension(file).ToLowerInvariant())
            {
                case ".png" or ".jpg" or ".jpeg" or ".gif":
                    library.Images.Add(new BundleImage(member, Bitmaps.Decode(File.ReadAllBytes(file), file), null, 0, 0, false, false, false));
                    break;
                case ".xml":
                    library.Documents.Add(new BundleDocument(member, BundleDocumentKind.Xml, HabLibraryReader.Text(File.ReadAllBytes(file))));
                    break;
                case ".bin" or ".txt":
                    var text = HabLibraryReader.Text(File.ReadAllBytes(file));
                    library.Documents.Add(new BundleDocument(member, text.TrimStart().StartsWith('<') ? BundleDocumentKind.Xml : BundleDocumentKind.Text, text));
                    break;
            }
        }
        return library;
    }

    /// <summary>Package folders and the class name, each an identifier: nothing that leaves the source.</summary>
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*(/[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex ClassPath();
}
