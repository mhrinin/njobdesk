using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Configuration;

namespace NJobDesk.AspNetCore.Hosting;

/// <summary>
/// Serves the embedded dashboard SPA. The index document (with the runtime config injected) is
/// built once at construction; hashed assets are streamed with immutable caching. Registered as a
/// singleton and injected into the <c>MapNJobDesk</c> endpoint handlers.
/// </summary>
internal sealed class NJobDeskDashboardAssets
{
    private const string ConfigToken = "<!--%NJOBDESK_CONFIG%-->";

    private readonly ManifestEmbeddedFileProvider _assets =
        new(Assembly.GetExecutingAssembly(), "assets");

    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    private readonly string? _index;

    public NJobDeskDashboardAssets(IOptions<NJobDeskDashboardOptions> options)
    {
        var file = _assets.GetFileInfo("index.html");
        if (!file.Exists)
        {
            return;
        }

        using var reader = new StreamReader(file.CreateReadStream());
        var apiBase = "/" + options.Value.ApiPath.Replace("{version:apiVersion}", "1").Trim('/');
        var readOnly = options.Value.ReadOnly ? "true" : "false";
        var config = $$"""<script>window.__NJOBDESK__ = { apiBase: "{{apiBase}}", readOnly: {{readOnly}} };</script>""";
        _index = reader.ReadToEnd().Replace(ConfigToken, config);
    }

    public IResult IndexResult(HttpContext context)
    {
        // The SPA uses relative asset paths, which only resolve under the base path with a trailing slash.
        if (context.Request.Path.Value?.EndsWith('/') is false)
        {
            return Results.Redirect(context.Request.Path + "/" + context.Request.QueryString, permanent: true);
        }

        if (_index is null)
        {
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "no-cache, no-store";
        return Results.Content(_index, "text/html");
    }

    public IResult AssetResult(string path, HttpContext context)
    {
        if (_assets.GetFileInfo(path) is { Exists: true, IsDirectory: false } file)
        {
            var contentType = _contentTypes.TryGetContentType(path, out var known) ? known : "application/octet-stream";
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.Stream(file.CreateReadStream(), contentType);
        }

        // The app has no client-side routes; send unknown deep links back to the dashboard root
        // (serving index.html in place would break its relative asset paths).
        var fullPath = context.Request.Path.Value ?? string.Empty;
        var basePath = fullPath[..^path.Length].TrimEnd('/');
        return Results.Redirect(basePath + "/", permanent: false);
    }
}
