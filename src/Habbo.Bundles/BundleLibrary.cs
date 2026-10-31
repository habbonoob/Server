namespace Habbo.Bundles;

/// <summary>An asset library as a bundle holds it: named documents and named images.</summary>
public sealed class BundleLibrary(string name) : IDisposable
{
    public string Name { get; } = name;

    public List<BundleDocument> Documents { get; } = [];

    public List<BundleImage> Images { get; } = [];

    public void Dispose()
    {
        foreach (var image in Images)
            image.Pixels?.Dispose();
    }
}
