using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vaultrift.Api.Realtime;

namespace Vaultrift.Api.Hosting;

public sealed class HeroInWorldHandler : IExceptionHandler
{
    public const string ProblemType = "hero-in-world";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not HeroInWorldException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = ProblemType,
                Title = "This hero is playing in a co-op world. Leave the world to play alone."
            },
            cancellationToken);

        return true;
    }
}
