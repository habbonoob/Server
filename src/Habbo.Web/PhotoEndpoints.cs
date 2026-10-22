using Habbo.Camera;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Habbo.Web;

/// <summary>Serves the camera's photos (/photos/{id}.png and the wall picture {id}_small.png, under
/// stories.image_url_base) and the rooms' navigator thumbnails (/navigator-thumbnail/{room id}.png, under
/// navigator.thumbnail.url_base), from the photo store.</summary>
public static class PhotoEndpoints
{
    private const string PngType = "image/png";

    public static void MapPhotoEndpoints(this WebApplication app)
    {
        app.MapGet(PhotoStore.PhotosPath + "{name}", (string name, PhotoStore store) =>
            store.FindPhoto(name) is { } file ? Results.File(file, PngType) : Results.NotFound());

        app.MapGet(PhotoStore.ThumbnailsPath + "{name}", (string name, PhotoStore store) =>
            store.FindThumbnail(name) is { } file ? Results.File(file, PngType) : Results.NotFound());
    }
}
