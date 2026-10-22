using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Habbo.Camera.Avatars;

/// <summary>Draws an avatar from its figure, as the client's avatar renderer does (AvatarImage, AvatarStructure,
/// AvatarImageCache): the body parts nearest the camera on top, each put together from its parts' bitmaps, mirrored for the
/// directions the libraries don't draw, and tinted with the figure's colours. The avatar stands (the default action),
/// facing any of the eight directions, its head turned on its own; the whole avatar or only its head. The parts come
/// from the asset mirror's libraries (<see cref="AssetCatalog"/>), the structure from the client's own files (seed/avatar)
/// and figuredata.</summary>
public sealed class AvatarImager
{
    /// <summary>The client's sizes: the room's ("h") and the zoomed-out room's ("sh"), which is drawn from the room's bitmaps
    /// at half size, as the client's "h_50" is.</summary>
    public const string LargeScale = "h";
    public const string SmallScale = "sh";

    private const string FullSet = "full";
    private const string HeadSet = "head";
    private const string HeadBodyPart = "head";

    /// <summary>The posture every part has a bitmap in, tried when the action's posture has none.</summary>
    private const string StandingPosture = "std";

    /// <summary>The angle each direction turns the avatar by (AvatarDirectionAngle).</summary>
    private static readonly int[] DirectionAngles = [45, 90, 135, 180, 225, 270, 315, 0];

    /// <summary>How many drawn avatars are kept; past it the cache starts over.</summary>
    private const int MaxCached = 4096;

    /// <summary>The most an avatar is scaled up.</summary>
    private const int MaxZoom = 4;

    private readonly AssetCatalog _assets;
    private readonly ILogger<AvatarImager> _logger;
    private readonly Lazy<AvatarStructure?> _structure;
    private readonly ConcurrentDictionary<string, byte[]?> _cache = new(StringComparer.Ordinal);

    /// <summary>The libraries' bitmaps are shared, and Skia doesn't draw one on several threads at once: one avatar at a
    /// time. Each takes a few milliseconds, and is kept once drawn.</summary>
    private readonly Lock _render = new();

    public AvatarImager(AssetCatalog assets, IOptions<CameraOptions> options, ILogger<AvatarImager> logger)
    {
        _assets = assets;
        _logger = logger;
        _structure = new Lazy<AvatarStructure?>(() => LoadStructure(options.Value));
    }

    /// <summary>The avatar as a PNG; null when the figure draws nothing (no part of it is known) or the structure files
    /// are missing.</summary>
    public byte[]? Draw(string figure, int direction, int headDirection, string scale, bool headOnly, int zoom = 1)
    {
        direction = Math.Clamp(direction, 0, 7);
        headDirection = Math.Clamp(headDirection, 0, 7);
        scale = scale == SmallScale ? SmallScale : LargeScale;
        zoom = Math.Clamp(zoom, 1, MaxZoom);
        var key = $"{figure}|{direction}|{headDirection}|{scale}|{headOnly}|{zoom}";
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        if (_cache.Count >= MaxCached)
            _cache.Clear();

        byte[]? png = null;
        try
        {
            lock (_render)
                png = Render(FigureString.Parse(figure), direction, headDirection, scale, headOnly, zoom);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not draw the avatar {Figure}.", figure);
        }
        return _cache[key] = png;
    }

    private byte[]? Render(FigureString figure, int direction, int headDirection, string scale, bool headOnly, int zoom)
    {
        if (_structure.Value is not { } structure)
            return null;
        var action = structure.Action;
        if (structure.Geometry.Canvas(scale, action.GeometryType) is not { } canvas)
            return null;

        var setId = headOnly ? HeadSet : FullSet;
        var bodyParts = structure.Geometry.BodyPartsAtAngle(setId, DirectionAngles[direction], action.GeometryType);
        using var image = new SKBitmap(new SKImageInfo(canvas.Width, canvas.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var surface = new SKCanvas(image))
        {
            surface.Clear(SKColors.Transparent);
            var drawn = SKRectI.Empty;
            // the farthest first, the nearest on top
            for (var i = bodyParts.Count - 1; i >= 0; i--)
            {
                // the head turns on its own; everything else faces the body's way
                var partDirection = bodyParts[i] == HeadBodyPart ? headDirection : direction;
                var layers = Layers(structure, figure, bodyParts[i], partDirection);
                using var bodyPart = RenderBodyPart(structure, canvas, scale, partDirection, layers);
                if (bodyPart is null)
                    continue;
                var x = (int)(bodyPart.X + canvas.OffsetX + canvas.RegX);
                var y = (int)(bodyPart.Y + canvas.OffsetY);
                Draw(surface, bodyPart.Bitmap, x, y, null);
                var rect = new SKRectI(x, y, x + bodyPart.Bitmap.Width, y + bodyPart.Bitmap.Height);
                drawn = drawn.IsEmpty ? rect : SKRectI.Union(drawn, rect);
            }
            if (drawn.IsEmpty)
                return null;
            // only the head: cut to what was drawn of it
            if (headOnly)
            {
                drawn.Intersect(new SKRectI(0, 0, canvas.Width, canvas.Height));
                using var head = new SKBitmap(new SKImageInfo(drawn.Width, drawn.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
                return image.ExtractSubset(head, drawn) ? Encode(head, zoom) : null;
            }
        }
        return Encode(image, zoom);
    }

    /// <summary>The parts a body part draws for the figure, in the geometry's order nearest first (AvatarStructure.getParts,
    /// for the default action, which animates nothing).</summary>
    private static List<AvatarPartLayer> Layers(AvatarStructure structure, FigureString figure, string bodyPart, int direction)
    {
        var geometryParts = structure.Geometry.PartsOf(structure.Action.GeometryType, bodyPart, direction);
        var hidden = new List<string>();
        var found = new List<AvatarPartLayer>();
        // the client keeps one colour across the loop: a part whose colour the figure doesn't give takes the last one
        uint? color = null;
        foreach (var type in figure.Types)
        {
            if (structure.FigureData.SetType(type) is not { } setType || !structure.FigureData.HasPalette(setType.PaletteId)
                || !setType.Sets.TryGetValue(figure.SetId(type), out var set))
                continue;
            hidden.AddRange(set.HiddenLayers);
            var colors = figure.Colors(type);
            foreach (var part in set.Parts.Where(p => geometryParts.Contains(p.Type)))
            {
                if (colors.Count > part.ColorIndex - 1)
                    color = part.ColorIndex > 0 ? structure.FigureData.Color(setType.PaletteId, colors[part.ColorIndex - 1]) : null;
                // eyes keep their own colours
                var colorable = part.ColorIndex > 0 && part.Type != "ey";
                found.Add(new AvatarPartLayer(part.Type, part.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), color, colorable,
                    structure.PartSets.FlippedType(part.Type)));
            }
        }
        return geometryParts.SelectMany(type => found.Where(layer => layer.Type == type && !hidden.Contains(type))).ToList();
    }

    /// <summary>A body part's parts put together into one image registered on the canvas (renderBodyPart).</summary>
    private AvatarBodyPartImage? RenderBodyPart(AvatarStructure structure, AvatarCanvas canvas, string scale, int direction, List<AvatarPartLayer> layers)
    {
        if (layers.Count == 0)
            return null;
        var mirrored = direction is 4 or 5 or 6;
        var posture = structure.Action.AssetPartDefinition;
        var images = new List<AvatarPartImage>();
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            // facing away, the face and eyes aren't seen
            if (direction == 7 && layer.Type is "fc" or "ey")
                continue;
            var type = layer.Type;
            var assetDirection = direction;
            var flipOnItsOwn = false;
            if (mirrored)
            {
                // hand items and the chest print are drawn mirrored as they are; everything else is drawn as the other side
                if (type is "ri" or "li" or "cp")
                    flipOnItsOwn = true;
                else
                {
                    assetDirection = direction switch { 4 => 2, 5 => 1, _ => 0 };
                    type = layer.FlippedType;
                }
            }
            // a small avatar is drawn from the large bitmaps and halved, as the client's "h_50" is: the libraries have
            // no small ones
            if (Resolve(LargeScale, posture, type, layer.Id, assetDirection, frame: 0) is not { } asset)
                continue;

            // the asset's offset, from its far edge when its library has it flipped (as the runtime's .hab reader gives it)
            var regX = asset.FlipH ? asset.Bitmap.Width + asset.OffsetX : asset.OffsetX;
            var regY = asset.FlipV ? asset.Bitmap.Height + asset.OffsetY : asset.OffsetY;
            if (flipOnItsOwn)
            {
                regX += scale == LargeScale ? 65 : 31;
                regX = -regX + asset.Bitmap.Width;
            }
            images.Add(new AvatarPartImage(asset.Bitmap, regX, regY, flipOnItsOwn, layer.IsColorable ? layer.Color : null));
        }
        if (images.Count == 0)
            return null;

        var (bitmap, unionRegX, unionRegY) = Union(images, mirrored);
        var small = scale == SmallScale;
        var baseY = small ? canvas.Height - 8 : canvas.Height - 16;
        double placeX = unionRegX, placeY = unionRegY;
        if (small)
        {
            (placeX, placeY) = (placeX / 2, placeY / 2);
            var half = Half(bitmap);
            bitmap.Dispose();
            bitmap = half;
        }
        var x = -placeX;
        var y = baseY - placeY;
        if (mirrored)
            x += small ? 31 : 67;
        // the client keeps a body part's place in whole pixels, cut towards zero
        return new AvatarBodyPartImage(bitmap, (int)x, (int)y);
    }

    /// <summary>A bitmap at half its size, smoothed, as the client's BitmapHelper.resampleBitmapData halves it.</summary>
    private static SKBitmap Half(SKBitmap bitmap)
    {
        var half = new SKBitmap(new SKImageInfo(Math.Max(1, (int)Math.Round(bitmap.Width * 0.5, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)Math.Round(bitmap.Height * 0.5, MidpointRounding.AwayFromZero)), SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(half);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height), new SKRect(0, 0, half.Width, half.Height),
            new SKSamplingOptions(SKFilterMode.Linear), null);
        return half;
    }

    /// <summary>The asset for a part, falling back as the client does: the frame, then the first frame, then both in the
    /// default posture.</summary>
    private LibraryAsset? Resolve(string scale, string posture, string type, string id, int direction, int frame)
    {
        foreach (var (p, f) in new[] { (posture, frame), (posture, 0), (StandingPosture, frame), (StandingPosture, 0) })
            if (_assets.Find($"{scale}_{p}_{type}_{id}_{direction}_{f}") is { } asset)
                return asset;
        return null;
    }

    /// <summary>The parts drawn together over the union of their rectangles (createUnionImage): the first drawn first,
    /// each mirrored when either it or the whole body part is, but not both. Returns the image and its registration point.</summary>
    private static (SKBitmap Bitmap, int RegX, int RegY) Union(List<AvatarPartImage> images, bool mirrored)
    {
        var bounds = SKRectI.Empty;
        foreach (var image in images)
            bounds = bounds.IsEmpty ? image.OffsetRect : SKRectI.Union(bounds, image.OffsetRect);
        var originX = -bounds.Left;
        var originY = -bounds.Top;
        var bitmap = new SKBitmap(new SKImageInfo(Math.Max(1, bounds.Width), Math.Max(1, bounds.Height), SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            foreach (var image in images)
            {
                var x = originX - image.RegX;
                var y = originY - image.RegY;
                if (mirrored)
                    x = bitmap.Width - (x + image.Bitmap.Width);
                using var paint = new SKPaint();
                if (image.Tint is { } tint)
                    paint.ColorFilter = Multiply(tint);
                if (mirrored != image.FlipH)
                {
                    canvas.Save();
                    canvas.Translate(x + image.Bitmap.Width, y);
                    canvas.Scale(-1, 1);
                    Draw(canvas, image.Bitmap, 0, 0, paint);
                    canvas.Restore();
                }
                else
                    Draw(canvas, image.Bitmap, x, y, paint);
            }
        }
        // a mirrored image registers from its far side
        return (bitmap, mirrored ? bitmap.Width - originX : originX, originY);
    }

    /// <summary>A bitmap at a whole pixel position, unscaled and unsmoothed.</summary>
    private static void Draw(SKCanvas canvas, SKBitmap bitmap, int x, int y, SKPaint? paint) =>
        canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height), SKRect.Create(x, y, bitmap.Width, bitmap.Height),
            new SKSamplingOptions(SKFilterMode.Nearest), paint);

    /// <summary>The colour as Flash's ColorTransform multiplies a part by it: each channel by the colour's, alpha kept.</summary>
    private static SKColorFilter Multiply(uint rgb) => SKColorFilter.CreateColorMatrix(
    [
        ((rgb >> 16) & 0xFF) / 255f, 0, 0, 0, 0,
        0, ((rgb >> 8) & 0xFF) / 255f, 0, 0, 0,
        0, 0, (rgb & 0xFF) / 255f, 0, 0,
        0, 0, 0, 1, 0,
    ]);

    /// <summary>The PNG, scaled up by whole pixels when zoomed.</summary>
    private static byte[] Encode(SKBitmap bitmap, int zoom)
    {
        if (zoom > 1)
        {
            using var zoomed = new SKBitmap(new SKImageInfo(bitmap.Width * zoom, bitmap.Height * zoom, SKColorType.Rgba8888, SKAlphaType.Premul));
            using (var canvas = new SKCanvas(zoomed))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height), new SKRect(0, 0, zoomed.Width, zoomed.Height),
                    new SKSamplingOptions(SKFilterMode.Nearest), null);
            }
            return Encode(zoomed, 1);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private AvatarStructure? LoadStructure(CameraOptions options)
    {
        var directory = CameraDirectories.Find(options.AvatarDirectory);
        var figureData = CameraDirectories.FindFile(options.FigureDataFile);
        if (directory is null || figureData is null)
        {
            _logger.LogWarning("No avatar structure at {Directory} and {FigureData}: avatars can't be drawn.", options.AvatarDirectory, options.FigureDataFile);
            return null;
        }
        return new AvatarStructure(
            AvatarGeometry.Load(Path.Combine(directory, "HabboAvatarGeometry.xml")),
            AvatarPartSets.Load(Path.Combine(directory, "HabboAvatarPartSets.xml")),
            AvatarAction.LoadDefault(Path.Combine(directory, "HabboAvatarActions.xml")),
            FigureData.Load(figureData));
    }

}
