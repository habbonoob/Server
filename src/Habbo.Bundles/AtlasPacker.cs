namespace Habbo.Bundles;

/// <summary>Packs rectangles into as few pages as it can: MaxRects, best short side fit, biggest first. Each image
/// keeps <see cref="Padding"/> transparent pixels to its right and below, so a scaled sprite never samples its
/// neighbour.</summary>
public sealed class AtlasPacker(int maxSize = AtlasPacker.DefaultMaxSize, int padding = AtlasPacker.DefaultPadding)
{
    /// <summary>A size every GPU takes, phones included.</summary>
    public const int DefaultMaxSize = 2048;
    public const int DefaultPadding = 1;

    public int Padding { get; } = padding;

    public AtlasLayout Pack(IReadOnlyList<(int Width, int Height)> sizes)
    {
        var placements = new AtlasPlacement[sizes.Count];
        var pages = new List<Page>();
        var order = Enumerable.Range(0, sizes.Count)
            .OrderByDescending(i => Math.Max(sizes[i].Width, sizes[i].Height))
            .ThenByDescending(i => sizes[i].Width * sizes[i].Height);
        foreach (var index in order)
        {
            var (width, height) = (sizes[index].Width + Padding, sizes[index].Height + Padding);
            if (width > maxSize + Padding || height > maxSize + Padding)
                throw new InvalidDataException($"An image of {sizes[index].Width}×{sizes[index].Height} does not fit a {maxSize} atlas.");

            var placed = false;
            for (var p = 0; p < pages.Count && !placed; p++)
            {
                if (pages[p].TryPlace(width, height, out var x, out var y))
                {
                    placements[index] = new AtlasPlacement(p, x, y);
                    placed = true;
                }
            }
            if (!placed)
            {
                var page = new Page(maxSize + Padding);
                pages.Add(page);
                page.TryPlace(width, height, out var x, out var y);
                placements[index] = new AtlasPlacement(pages.Count - 1, x, y);
            }
        }
        return new AtlasLayout(placements, pages.Select(p => (Math.Max(1, p.UsedWidth - Padding), Math.Max(1, p.UsedHeight - Padding))).ToList());
    }

    private sealed class Page(int size)
    {
        private readonly List<(int X, int Y, int W, int H)> _free = [(0, 0, size, size)];

        public int UsedWidth { get; private set; }
        public int UsedHeight { get; private set; }

        public bool TryPlace(int width, int height, out int x, out int y)
        {
            (x, y) = (0, 0);
            var best = -1;
            var bestShort = int.MaxValue;
            var bestLong = int.MaxValue;
            for (var i = 0; i < _free.Count; i++)
            {
                var free = _free[i];
                if (free.W < width || free.H < height)
                    continue;
                var shortSide = Math.Min(free.W - width, free.H - height);
                var longSide = Math.Max(free.W - width, free.H - height);
                if (shortSide < bestShort || (shortSide == bestShort && longSide < bestLong))
                    (best, bestShort, bestLong) = (i, shortSide, longSide);
            }
            if (best < 0)
                return false;

            (x, y) = (_free[best].X, _free[best].Y);
            Split(x, y, width, height);
            UsedWidth = Math.Max(UsedWidth, x + width);
            UsedHeight = Math.Max(UsedHeight, y + height);
            return true;
        }

        private void Split(int x, int y, int width, int height)
        {
            var created = new List<(int X, int Y, int W, int H)>();
            for (var i = _free.Count - 1; i >= 0; i--)
            {
                var f = _free[i];
                if (x >= f.X + f.W || x + width <= f.X || y >= f.Y + f.H || y + height <= f.Y)
                    continue;
                _free.RemoveAt(i);
                if (x > f.X) created.Add((f.X, f.Y, x - f.X, f.H));
                if (x + width < f.X + f.W) created.Add((x + width, f.Y, f.X + f.W - (x + width), f.H));
                if (y > f.Y) created.Add((f.X, f.Y, f.W, y - f.Y));
                if (y + height < f.Y + f.H) created.Add((f.X, y + height, f.W, f.Y + f.H - (y + height)));
            }
            _free.AddRange(created);
            // a free rectangle inside another adds nothing
            for (var i = _free.Count - 1; i >= 0; i--)
            {
                for (var j = 0; j < _free.Count; j++)
                {
                    if (i != j && Contains(_free[j], _free[i]))
                    {
                        _free.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static bool Contains((int X, int Y, int W, int H) outer, (int X, int Y, int W, int H) inner) =>
            inner.X >= outer.X && inner.Y >= outer.Y && inner.X + inner.W <= outer.X + outer.W && inner.Y + inner.H <= outer.Y + outer.H;
    }
}
