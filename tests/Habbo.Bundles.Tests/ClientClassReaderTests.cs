using SkiaSharp;

namespace Habbo.Bundles.Tests;

public sealed class ClientClassReaderTests : IDisposable
{
    private readonly string _source = Directory.CreateTempSubdirectory("client-source").FullName;

    public void Dispose() => Directory.Delete(_source, recursive: true);

    [Fact]
    public void AClassIsWhatItEmbedsNamedAfterItsMembers()
    {
        Directory.CreateDirectory(Path.Combine(_source, "_assets"));
        Directory.CreateDirectory(Path.Combine(_source, "splash"));
        File.WriteAllText(Path.Combine(_source, "splash", "PhotoSplashScreen.as"), """
            public class PhotoSplashScreen extends Sprite
            {
                  [Embed(source="/_assets/splashBgClass.png")]
                  private var splashBgClass:Class;
                  [Embed(source="/_assets/splash_texts.bin", mimeType="application/octet-stream")]
                  private var splashTexts:Class;
                  [Embed(source="/_assets/font.ttf", fontName="Ubuntu")]
                  private var font:Class;
            }
            """);
        File.WriteAllBytes(Path.Combine(_source, "_assets", "splashBgClass.png"), TestImages.Png(3, 2, (_, _) => SKColors.Navy));
        File.WriteAllText(Path.Combine(_source, "_assets", "splash_texts.bin"), "a=b");
        File.WriteAllBytes(Path.Combine(_source, "_assets", "font.ttf"), [0, 1, 0, 0]);

        using var library = ClientClassReader.Read(_source, "splash/PhotoSplashScreen");

        Assert.Equal("splash.PhotoSplashScreen", library.Name);
        Assert.Equal(("splashBgClass", 3), (Assert.Single(library.Images).Name, library.Images[0].Pixels!.Width));
        Assert.Equal(new BundleDocument("splashTexts", BundleDocumentKind.Text, "a=b"), Assert.Single(library.Documents));

        using var only = ClientClassReader.Read(_source, "splash/PhotoSplashScreen", new HashSet<string> { "splashTexts" });
        Assert.Empty(only.Images);
        Assert.Single(only.Documents);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("splash//X")]
    [InlineData("/abs")]
    public void APathThatLeavesTheSourceIsRefused(string path) =>
        Assert.Throws<ArgumentException>(() => ClientClassReader.Read(_source, path));
}
