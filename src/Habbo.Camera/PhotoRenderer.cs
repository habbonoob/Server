using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Habbo.Camera;

/// <summary>Renders a photo from what the client composed (RenderRoomMessageComposer): the room's planes and the
/// sprites in view, already placed on the photo's canvas by the client, drawn far to near; then the photo lab's zoom
/// and effects, as CameraPhotoLab applies them to its preview.
///
/// The canvas is the background plane's (the first, the room's background colour over the whole photo). A plane is
/// its texture — its columns of assets, tinted with its colour, with the masks of the windows on it cut out — laid
/// on the quad its corners make; a sprite is its asset (or, for a poster or photo, its picture) at its place, mirrored,
/// tinted, faded and blended as the client drew it.</summary>
public sealed class PhotoRenderer(AssetCatalog assets, ExternalImages images, IOptions<CameraOptions> options, ILogger<PhotoRenderer> logger)
{
    /// <summary>A photo furni's picture on the wall: the client scales it to the furni, so this only bounds it.</summary>
    public const int SmallSize = 100;

    /// <summary>The photo's size when the request has no background plane to take it from (the camera's viewfinder).</summary>
    private const int DefaultSize = 320;

    /// <summary>How much taller than wide a picture on a wall is drawn (IsometricImageFurniVisualization).</summary>
    private const float WallPictureStretch = 1.1f;

    /// <summary>Habbo is drawn pixel for pixel: nothing smoothed, as the client draws.</summary>
    private static readonly SKSamplingOptions Pixel = new(SKFilterMode.Nearest);

    /// <summary>The most sprites drawn: a photo the client sends has a few hundred.</summary>
    private const int MaxSprites = 4096;

    private const int MaxPlanes = 512;

    /// <summary>The largest side of a plane's texture: the canvas's own largest. The corners are the client's to say, so a
    /// plane bigger than that is drawn in its flat colour.</summary>
    private const int MaxTextureSize = 2048;

    /// <summary>The most texture pixels one photo draws, all planes together (four of the largest canvases); a room's
    /// photo needs a few million. Planes past it are drawn in their flat colour.</summary>
    private const long MaxTexturePixels = 4L * MaxTextureSize * MaxTextureSize;

    /// <summary>The most photo lab effects applied: the lab offers a handful at once.</summary>
    private const int MaxFilters = 16;

    /// <summary>How many photos are drawn at once, across the process: each takes a canvas and its textures' memory.</summary>
    private static readonly SemaphoreSlim Rendering = new(2);

    public async Task<RenderedPhoto> RenderAsync(RenderRequest request, CancellationToken ct)
    {
        await Rendering.WaitAsync(ct);
        try
        {
            return await RenderOneAsync(request, ct);
        }
        finally
        {
            Rendering.Release();
        }
    }

    private async Task<RenderedPhoto> RenderOneAsync(RenderRequest request, CancellationToken ct)
    {
        var (width, height) = CanvasSize(request.Planes);
        var pictures = await LoadPicturesAsync(request, ct);

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul))
                            ?? throw new InvalidOperationException($"Could not make a {width}x{height} canvas.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        // far to near: the client's order is by depth, the largest first; planes and sprites share the one depth
        var drawables = request.Planes.Take(MaxPlanes).Select((plane, i) => (Z: plane.Z, Order: i, Plane: (RenderPlane?)plane, Sprite: (RenderSprite?)null))
            .Concat(request.Sprites.Take(MaxSprites).Select((sprite, i) => (Z: sprite.Z, Order: MaxPlanes + i, Plane: (RenderPlane?)null, Sprite: (RenderSprite?)sprite)))
            .OrderByDescending(d => d.Z).ThenBy(d => d.Order);
        var texturePixels = MaxTexturePixels;
        foreach (var drawable in drawables)
        {
            ct.ThrowIfCancellationRequested();
            if (drawable.Plane is { } plane)
                DrawPlane(canvas, plane, ref texturePixels);
            else
                DrawSprite(canvas, drawable.Sprite!, pictures);
        }

        using var photo = surface.Snapshot();
        using var finished = await FinishAsync(photo, request, ct);
        using var small = Scale(finished, SmallSize, SmallSize);
        return new RenderedPhoto(Encode(finished), Encode(small));
    }

    /// <summary>The background plane covers the whole photo: its corners give its size.</summary>
    private static (int Width, int Height) CanvasSize(IReadOnlyList<RenderPlane> planes)
    {
        if (planes.Count == 0 || planes[0].CornerPoints.Count != 4)
            return (DefaultSize, DefaultSize);
        var width = planes[0].CornerPoints.Max(p => p.X);
        var height = planes[0].CornerPoints.Max(p => p.Y);
        return width is > 0 and <= MaxTextureSize && height is > 0 and <= MaxTextureSize ? (width, height) : (DefaultSize, DefaultSize);
    }

    /// <summary>The pictures the sprites name by url, read before drawing (they are fetched).</summary>
    private async Task<Dictionary<string, SKBitmap>> LoadPicturesAsync(RenderRequest request, CancellationToken ct)
    {
        var urls = request.Sprites.Take(MaxSprites).Select(s => s.Name)
            .Where(name => name.StartsWith("http", StringComparison.Ordinal) || name.StartsWith("//", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).ToList();
        var loaded = await Task.WhenAll(urls.Select(async url => (Url: url, Image: await images.GetAsync(url).WaitAsync(ct))));
        return loaded.Where(l => l.Image is not null).ToDictionary(l => l.Url, l => l.Image!, StringComparer.Ordinal);
    }

    /// <summary>Draws a plane; its texture's pixels come out of what is left to draw (<paramref name="texturePixels"/>).</summary>
    private void DrawPlane(SKCanvas canvas, RenderPlane plane, ref long texturePixels)
    {
        if (plane.CornerPoints.Count != 4)
            return;
        // the client sorts the corners (SpriteDataCollector.sortQuadPoints): the lower edge's right then left, the
        // upper edge's right then left
        var p = plane.CornerPoints.Select(c => new SKPoint(c.X, c.Y)).ToArray();
        var (bottomLeft, topRight, topLeft) = (p[1], p[2], p[3]);
        var color = Opaque(plane.Color);

        using var texture = plane.TexCols is { Count: > 0 } columns ? Texture(plane, columns, topLeft, topRight, bottomLeft, color, ref texturePixels) : null;
        if (texture is null)
        {
            using var fill = new SKPaint { Color = color, IsAntialias = false };
            using var builder = new SKPathBuilder();
            builder.MoveTo(p[3]);
            builder.LineTo(p[2]);
            builder.LineTo(p[0]);
            builder.LineTo(p[1]);
            builder.Close();
            using var quad = builder.Detach();
            canvas.DrawPath(quad, fill);
            return;
        }

        // the texture's corners on the quad's: its top left on the upper left corner, its width along the upper
        // edge, its height along the left edge
        var u = new SKPoint((topRight.X - topLeft.X) / texture.Width, (topRight.Y - topLeft.Y) / texture.Width);
        var v = new SKPoint((bottomLeft.X - topLeft.X) / texture.Height, (bottomLeft.Y - topLeft.Y) / texture.Height);
        var matrix = new SKMatrix(u.X, v.X, topLeft.X, u.Y, v.Y, topLeft.Y, 0, 0, 1);
        canvas.Save();
        canvas.Concat(in matrix);
        canvas.DrawBitmap(texture, 0, 0, Pixel, null);
        canvas.Restore();
    }

    /// <summary>A plane's texture, in its own flat space: its columns side by side, each its assets stacked (from the
    /// bottom when the material is bottom aligned), repeated down the plane unless the column ends in "" (drawn
    /// once); tinted with the plane's colour, and the windows' masks cut out.</summary>
    private SKBitmap? Texture(RenderPlane plane, IReadOnlyList<RenderTextureColumn> columns, SKPoint topLeft, SKPoint topRight, SKPoint bottomLeft, SKColor color, ref long texturePixels)
    {
        // a wall's width runs along a slant: its texture is as wide as the slant is across, as tall as the wall is
        // high; a floor's sides both slant, and its texture is as long as each is across
        var top = new SKPoint(topRight.X - topLeft.X, topRight.Y - topLeft.Y);
        var side = new SKPoint(bottomLeft.X - topLeft.X, bottomLeft.Y - topLeft.Y);
        var width = (int)Math.Round(Math.Abs(top.X) > 0 ? Math.Abs(top.X) : Math.Abs(top.Y));
        var height = (int)Math.Round(Math.Abs(side.X) > 0 ? Math.Abs(side.X) : Math.Abs(side.Y));
        if (width <= 0 || height <= 0 || width > MaxTextureSize || height > MaxTextureSize || (long)width * height > texturePixels)
            return null;
        texturePixels -= (long)width * height;

        var texture = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(texture);
        canvas.Clear(SKColors.Transparent);
        // each column as wide as its material says (an asset wider than that is cut to it), the columns repeated
        // across the plane, as the client's material matrix repeats them
        var laid = columns.Select(column => (
                Cells: column.AssetNames.Where(n => n.Length > 0).Select(n => assets.Find(n)).OfType<LibraryAsset>().ToList(),
                Width: column.AssetNames.Where(n => n.Length > 0).Select(assets.ColumnWidth).FirstOrDefault(w => w is > 0),
                Repeated: column.AssetNames.Count == 0 || column.AssetNames[^1].Length != 0))
            .Where(c => c.Cells.Count > 0 && c.Cells.Sum(cell => cell.Bitmap.Height) > 0)
            .Select(c => (c.Cells, Width: c.Width ?? c.Cells.Max(cell => cell.Bitmap.Width), c.Repeated))
            .ToList();
        if (laid.Count == 0)
            return null;

        for (var (x, at) = (0, 0); x < width; x += laid[at].Width, at = (at + 1) % laid.Count)
        {
            var (cells, columnWidth, repeated) = laid[at];
            canvas.Save();
            canvas.ClipRect(new SKRect(x, 0, x + columnWidth, height));
            if (plane.BottomAligned)
            {
                var y = height;
                do
                {
                    for (var i = cells.Count - 1; i >= 0 && y > 0; i--)
                    {
                        y -= cells[i].Bitmap.Height;
                        DrawAsset(canvas, cells[i], x, y);
                    }
                } while (repeated && y > 0);
            }
            else
            {
                var y = 0;
                do
                {
                    foreach (var cell in cells)
                    {
                        if (y >= height)
                            break;
                        DrawAsset(canvas, cell, x, y);
                        y += cell.Bitmap.Height;
                    }
                } while (repeated && y < height);
            }
            canvas.Restore();
        }

        // the plane's colour multiplies its texture, as the client's colour transform does
        using (var tint = new SKPaint { ColorFilter = Multiply(color), BlendMode = SKBlendMode.Src })
        using (var copy = texture.Copy())
            canvas.DrawBitmap(copy, 0, 0, Pixel, tint);

        // a window's mask is opaque where the wall is cut away
        foreach (var mask in plane.Masks ?? [])
        {
            if (assets.Find(mask.Name) is not { } asset)
                continue;
            using var cut = new SKPaint { BlendMode = SKBlendMode.DstOut };
            DrawMirrored(canvas, asset.Bitmap, mask.Location.X, mask.Location.Y, mask.FlipH, mask.FlipV, cut);
        }
        return texture;
    }

    private static void DrawAsset(SKCanvas canvas, LibraryAsset asset, int x, int y) =>
        DrawMirrored(canvas, asset.Bitmap, x, y, asset.FlipH, asset.FlipV, null);

    /// <summary>A bitmap with its top left at (x, y), mirrored in place.</summary>
    private static void DrawMirrored(SKCanvas canvas, SKBitmap bitmap, float x, float y, bool flipH, bool flipV, SKPaint? paint)
    {
        canvas.Save();
        canvas.Translate(flipH ? x + bitmap.Width : x, flipV ? y + bitmap.Height : y);
        canvas.Scale(flipH ? -1 : 1, flipV ? -1 : 1);
        canvas.DrawBitmap(bitmap, 0, 0, Pixel, paint);
        canvas.Restore();
    }

    private void DrawSprite(SKCanvas canvas, RenderSprite sprite, IReadOnlyDictionary<string, SKBitmap> pictures)
    {
        if (string.IsNullOrEmpty(sprite.Name))
            return;
        if (sprite.Name.StartsWith("http", StringComparison.Ordinal) || sprite.Name.StartsWith("//", StringComparison.Ordinal))
        {
            DrawPicture(canvas, sprite, pictures.GetValueOrDefault(sprite.Name));
            return;
        }

        // a pet's sprite names its palette after an @
        var at = sprite.Name.IndexOf('@', StringComparison.Ordinal);
        var name = at < 0 ? sprite.Name : sprite.Name[..at];
        if (assets.Find(name) is not { } asset)
            return;

        using var palette = at >= 0 && sprite.PaletteSourceName is { } source ? Colorize(asset.Bitmap, assets.FindBytes(name, source)) : null;
        using var paint = SpritePaint(sprite);
        DrawMirrored(canvas, palette ?? asset.Bitmap, (float)Math.Round(sprite.X), (float)Math.Round(sprite.Y), sprite.FlipH, false, paint);
    }

    /// <summary>A poster's or photo's picture: scaled to the sprite, slanted onto the wall when the client slanted it
    /// (a photo on a wall: skew), with its dark outline when it has one (a photo's frame); its average colour when
    /// the picture could not be had.</summary>
    private static void DrawPicture(SKCanvas canvas, RenderSprite sprite, SKBitmap? picture)
    {
        var width = sprite.Width is > 0 ? sprite.Width.Value : picture?.Width ?? 0;
        var height = sprite.Height is > 0 ? sprite.Height.Value : picture?.Height ?? 0;
        if (width <= 0 || height <= 0)
            return;

        var sourceWidth = picture?.Width ?? width;
        var sourceHeight = picture?.Height ?? height;
        var scale = (float)width / sourceWidth;
        var matrix = SKMatrix.CreateScale(scale, scale);
        if (sprite.Skew is { } skew && skew != 0)
        {
            // as IsometricImageFurniVisualization slants it: a half pixel down per pixel across, stretched a tenth taller
            var slant = (float)(-skew) * scale;
            matrix = new SKMatrix(scale, 0, 0, slant, scale * WallPictureStretch, slant < 0 ? -slant * sourceWidth : 0, 0, 0, 1);
        }
        matrix = matrix.PostConcat(SKMatrix.CreateTranslation((float)Math.Round(sprite.X), (float)Math.Round(sprite.Y)));

        canvas.Save();
        canvas.Concat(in matrix);
        if (picture is null)
        {
            using var fill = new SKPaint { Color = Opaque((uint)(sprite.Color ?? 0x808080)) };
            canvas.DrawRect(0, 0, sourceWidth, sourceHeight, fill);
        }
        else
        {
            if (sprite.Frame)
            {
                using var outline = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(SKColors.Black, SKBlendMode.SrcIn) };
                foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                    canvas.DrawBitmap(picture, dx / scale, dy / scale, Pixel, outline);
            }
            using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)Math.Clamp(sprite.Alpha ?? 255, 0, 255)) };
            canvas.DrawBitmap(picture, 0, 0, Pixel, paint);
        }
        canvas.Restore();
    }

    private static SKPaint SpritePaint(RenderSprite sprite)
    {
        var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)Math.Clamp(sprite.Alpha ?? 255, 0, 255)),
            BlendMode = BlendMode(sprite.BlendMode),
        };
        if (sprite.Color is { } color && (color & 0xFFFFFF) != 0xFFFFFF)
            paint.ColorFilter = Multiply(Opaque((uint)color));
        return paint;
    }

    /// <summary>A pet's colours: its palette (RGB triples) indexed by each pixel's green, its alpha kept
    /// (GraphicAssetPalette.colorizeBitmap).</summary>
    private static SKBitmap? Colorize(SKBitmap bitmap, byte[]? palette)
    {
        if (palette is null || palette.Length < 3)
            return null;
        var colored = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using var source = bitmap.Copy(SKColorType.Rgba8888);
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var pixel = source.GetPixel(x, y);
            var index = pixel.Green * 3;
            colored.SetPixel(x, y, index + 2 < palette.Length
                ? new SKColor(palette[index], palette[index + 1], palette[index + 2], pixel.Alpha)
                : new SKColor(0, 0, 0, pixel.Alpha));
        }
        return colored;
    }

    /// <summary>The photo lab's work on the photo (CameraPhotoLab.renderAllEffects): the zoom, then each effect in
    /// the order chosen — a colour matrix blended with none by its strength, an image composited at its strength —
    /// then the frames over everything.</summary>
    private async Task<SKImage> FinishAsync(SKImage photo, RenderRequest request, CancellationToken ct)
    {
        var info = new SKImageInfo(photo.Width, photo.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("Could not make the photo's canvas.");
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);
        if (request.Zoom > 1)
        {
            // as the lab zooms: twice the size, its centre kept
            canvas.Save();
            canvas.Translate(-photo.Width / 2f * ((float)request.Zoom - 1), -photo.Height / 2f * ((float)request.Zoom - 1));
            canvas.Scale((float)request.Zoom);
            canvas.DrawImage(photo, 0, 0, Pixel, null);
            canvas.Restore();
        }
        else
        {
            canvas.DrawImage(photo, 0, 0, Pixel, null);
        }

        var filters = (request.Filters ?? []).Take(MaxFilters).ToList();
        foreach (var filter in filters.Where(f => CameraEffects.All.TryGetValue(f.Name, out var e) && e.Kind != CameraEffectKind.Frame))
        {
            var effect = CameraEffects.All[filter.Name];
            var strength = Math.Clamp((filter.Alpha ?? 255) / 255f, 0, 1);
            if (effect.Kind == CameraEffectKind.ColorMatrix)
            {
                using var snapshot = surface.Snapshot();
                using var paint = new SKPaint { ColorFilter = SKColorFilter.CreateColorMatrix(Blend(effect.Matrix!, strength)), BlendMode = SKBlendMode.Src };
                canvas.DrawImage(snapshot, 0, 0, Pixel, paint);
            }
            else if (await EffectImageAsync(filter.Name, ct) is { } image)
            {
                using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(strength * 255)), BlendMode = BlendMode(effect.BlendMode) };
                canvas.DrawBitmap(image, 0, 0, Pixel, paint);
            }
        }
        foreach (var filter in filters.Where(f => CameraEffects.All.TryGetValue(f.Name, out var e) && e.Kind == CameraEffectKind.Frame))
            if (await EffectImageAsync(filter.Name, ct) is { } frame)
                canvas.DrawBitmap(frame, 0, 0, Pixel, null);

        return surface.Snapshot();
    }

    private async Task<SKBitmap?> EffectImageAsync(string name, CancellationToken ct)
    {
        var image = await images.GetOwnAsync(options.Value.EffectImagePath + name + ".png").WaitAsync(ct);
        if (image is null)
            logger.LogWarning("The photo lab's effect image {Name} is not in the mirror: the photo is rendered without it.", name);
        return image;
    }

    /// <summary>The effect's matrix at a strength: blended with the identity (CameraEffect.getColorMatrixFilter), its
    /// offsets from Flash's 0-255 to Skia's 0-1.</summary>
    private static float[] Blend(float[] matrix, float strength)
    {
        float[] identity = [1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0];
        var blended = new float[20];
        for (var i = 0; i < 20; i++)
        {
            blended[i] = matrix[i] * strength + identity[i] * (1 - strength);
            if (i % 5 == 4)
                blended[i] /= 255f;
        }
        return blended;
    }

    /// <summary>Flash's colour transform with these multipliers: each channel times the colour's.</summary>
    private static SKColorFilter Multiply(SKColor color) => SKColorFilter.CreateColorMatrix(
    [
        color.Red / 255f, 0, 0, 0, 0,
        0, color.Green / 255f, 0, 0, 0,
        0, 0, color.Blue / 255f, 0, 0,
        0, 0, 0, 1, 0,
    ]);

    /// <summary>Flash's blend modes as Skia's.</summary>
    private static SKBlendMode BlendMode(string? mode) => mode switch
    {
        "add" => SKBlendMode.Plus,
        "multiply" => SKBlendMode.Multiply,
        "screen" => SKBlendMode.Screen,
        "lighten" => SKBlendMode.Lighten,
        "darken" => SKBlendMode.Darken,
        "difference" => SKBlendMode.Difference,
        // Skia has no subtraction; its difference is the same wherever the sprite is the darker
        "subtract" => SKBlendMode.Difference,
        "overlay" => SKBlendMode.Overlay,
        "hardlight" => SKBlendMode.HardLight,
        _ => SKBlendMode.SrcOver,
    };

    /// <summary>A colour as the client gives it (RGB, its alpha byte unused) made opaque.</summary>
    private static SKColor Opaque(uint rgb) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

    private static SKImage Scale(SKImage image, int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul))!;
        surface.Canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        return surface.Snapshot();
    }

    private static byte[] Encode(SKImage image)
    {
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
