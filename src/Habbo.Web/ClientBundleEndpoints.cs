using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Habbo.Web;

/// <summary>The client's own libraries as habbo-client's bundles (<see cref="AssetBundles"/>): the components of the
/// client binary under /client/components/{component}, what one class embeds under /client/classes/{path}, and the libraries beside it under /client/local/{library}.
/// Only <c>?format=bundle</c> is served: the client's files themselves are not.</summary>
public static class ClientBundleEndpoints
{
    public static void MapClientBundleEndpoints(this WebApplication app)
    {
        app.MapGet("/client/components/{component}", async (HttpContext context, string component, string? format, AssetBundles bundles, CancellationToken ct) =>
            format == MirrorEndpoints.BundleFormat && await bundles.ComponentAsync(component, ct) is { } bundle
                ? MirrorEndpoints.Bundle(context, bundle)
                : Results.NotFound()).RequireRateLimiting(WebRateLimits.Mirror);
        app.MapGet("/client/classes/{**classPath}", async (HttpContext context, string classPath, string? format, string? members, AssetBundles bundles,
                CancellationToken ct) =>
            format == MirrorEndpoints.BundleFormat && await bundles.ClassAsync(classPath, members, ct) is { } bundle
                ? MirrorEndpoints.Bundle(context, bundle)
                : Results.NotFound()).RequireRateLimiting(WebRateLimits.Mirror);
        app.MapGet("/client/local/{library}", async (HttpContext context, string library, string? format, AssetBundles bundles, CancellationToken ct) =>
            format == MirrorEndpoints.BundleFormat && await bundles.LocalAsync(library, ct) is { } bundle
                ? MirrorEndpoints.Bundle(context, bundle)
                : Results.NotFound()).RequireRateLimiting(WebRateLimits.Mirror);
    }
}
