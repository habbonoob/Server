using SkiaSharp;

namespace Habbo.Bundles.Tests;

public sealed class ClientComponentReaderTests : IDisposable
{
    private readonly string _source = Directory.CreateTempSubdirectory("client-source").FullName;

    public void Dispose() => Directory.Delete(_source, recursive: true);

    [Fact]
    public void AComponentIsTheAssetsItsManifestNamesAsItsClassEmbedsThem()
    {
        Directory.CreateDirectory(Path.Combine(_source, "_assets"));
        File.WriteAllText(Path.Combine(_source, "HabboTestCom.as"), """
            public class HabboTestCom
            {
                  [Embed(source="/_assets/HabboTestCom_manifest.bin", mimeType="application/octet-stream")]

                  public static var manifest:Class;

                  [Embed(source="/_assets/test_layout.bin", mimeType="application/octet-stream")]

                  public static const test_layout_xml:Class;

                  [Embed(source="/_assets/stars.png")]

                  public static const star_sheet:Class;
            }
            """);
        File.WriteAllText(Path.Combine(_source, "_assets", "HabboTestCom_manifest.bin"), """
            <manifest><component><assets>
                <asset mimeType="text/xml" name="test_layout_xml"/>
                <asset mimeType="image/png" name="star_1" ref="star_sheet"><param key="region" value="2,0,2,2"/><param key="offset" value="3,-1"/></asset>
                <asset mimeType="image/png" name="never_embedded"/>
            </assets></component></manifest>
            """);
        File.WriteAllText(Path.Combine(_source, "_assets", "test_layout.bin"), "<window/>");
        File.WriteAllBytes(Path.Combine(_source, "_assets", "stars.png"), TestImages.Png(4, 2, (x, _) => x < 2 ? SKColors.Red : SKColors.Yellow));

        using var library = ClientComponentReader.Read(_source, "HabboTestCom");

        Assert.Equal(["manifest", "test_layout_xml"], library.Documents.Select(d => d.Name).Order());
        Assert.Equal("<window/>", library.Documents.Single(d => d.Name == "test_layout_xml").Text);
        var star = Assert.Single(library.Images);
        Assert.Equal(("star_1", 2, 2, 3, -1, true), (star.Name, star.Pixels!.Width, star.Pixels.Height, star.OffsetX, star.OffsetY, star.HasOffset));
        Assert.Equal(SKColors.Yellow, star.Pixels.GetPixel(0, 0));
    }

    [Fact]
    public void ANameThatIsNotAComponentIsRefused() =>
        Assert.Throws<ArgumentException>(() => ClientComponentReader.Read(_source, "../etc"));
}
