namespace Habbo.Bundles.Tests;

public class AtlasPackerTests
{
    [Fact]
    public void ImagesNeverOverlapAndKeepTheirPaddingInsideThePage()
    {
        var random = new Random(7);
        var sizes = Enumerable.Range(0, 400).Select(_ => (random.Next(1, 90), random.Next(1, 90))).ToList();

        var layout = new AtlasPacker(256, padding: 1).Pack(sizes);

        for (var i = 0; i < sizes.Count; i++)
        {
            var a = layout.Placements[i];
            var (width, height) = layout.Pages[a.Page];
            Assert.InRange(a.X + sizes[i].Item1, 0, width);
            Assert.InRange(a.Y + sizes[i].Item2, 0, height);
            for (var j = i + 1; j < sizes.Count; j++)
            {
                var b = layout.Placements[j];
                // each counted with its padding: they may touch, never overlap
                var apart = a.Page != b.Page
                    || a.X + sizes[i].Item1 + 1 <= b.X || b.X + sizes[j].Item1 + 1 <= a.X
                    || a.Y + sizes[i].Item2 + 1 <= b.Y || b.Y + sizes[j].Item2 + 1 <= a.Y;
                Assert.True(apart, $"{i} and {j} overlap");
            }
        }
        Assert.True(layout.Pages.Count > 1, "400 images should not fit one 256 page");
    }

    [Fact]
    public void APageIsCutToWhatItUses()
    {
        var layout = new AtlasPacker().Pack([(10, 20), (5, 5)]);

        Assert.Single(layout.Pages);
        Assert.True(layout.Pages[0].Width < 64 && layout.Pages[0].Height < 64);
    }

    [Fact]
    public void AnImageBiggerThanAPageIsRefused() =>
        Assert.Throws<InvalidDataException>(() => new AtlasPacker(64).Pack([(65, 10)]));
}
