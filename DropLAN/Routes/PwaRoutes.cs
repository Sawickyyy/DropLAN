using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DropLAN.Routes;

public static class PwaRoutes
{
    public static void MapPwaRoutes(
        this WebApplication app,
        string webAssetsPath)
    {
        app.MapGet("/", () => Results.File(
            Path.Combine(webAssetsPath, "index.html"),
            "text/html; charset=utf-8"));

        MapAsset(app, webAssetsPath, "/style.css", "style.css", "text/css; charset=utf-8");
        MapAsset(app, webAssetsPath, "/app.js", "app.js", "application/javascript; charset=utf-8");
        MapAsset(app, webAssetsPath, "/manifest.webmanifest", "manifest.webmanifest", "application/manifest+json");
        MapAsset(app, webAssetsPath, "/sw.js", "sw.js", "application/javascript; charset=utf-8");
        MapAsset(app, webAssetsPath, "/icon-192.png", "icon-192.png", "image/png");
        MapAsset(app, webAssetsPath, "/icon-512.png", "icon-512.png", "image/png");
        MapAsset(app, webAssetsPath, "/apple-touch-icon.png", "apple-touch-icon.png", "image/png");
    }

    private static void MapAsset(
        WebApplication app,
        string root,
        string route,
        string fileName,
        string contentType)
    {
        app.MapGet(route, () => Results.File(
            Path.Combine(root, fileName),
            contentType));
    }
}
