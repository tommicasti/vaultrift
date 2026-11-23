using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Vaultrift.Api.Hosting;

public static class StaticSetup
{
    private const string Forever = "public,max-age=31536000,immutable";
    private const string OneDay = "public,max-age=86400";
    private const string Revalidate = "no-cache";

    public static WebApplication UseGameClient(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = ContentTypes(),
            OnPrepareResponse = context =>
                context.Context.Response.Headers[HeaderNames.CacheControl] = CachePolicy(context.Context.Request.Path)
        });

        return app;
    }

    public static WebApplication MapGameClientFallback(this WebApplication app)
    {
        app.Map("/api/{**rest}", () => Results.NotFound());
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers[HeaderNames.CacheControl] = Revalidate
        });

        return app;
    }

    public static FileExtensionContentTypeProvider ContentTypes()
    {
        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".glb"] = "model/gltf-binary";
        return provider;
    }

    private static string CachePolicy(PathString path)
    {
        if (path.StartsWithSegments("/assets"))
            return Forever;

        return path.Value?.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == true || path == "/"
            ? Revalidate
            : OneDay;
    }
}
