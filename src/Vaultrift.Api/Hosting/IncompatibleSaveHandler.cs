using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vaultrift.Infrastructure.Persistence;

namespace Vaultrift.Api.Hosting;

public sealed class IncompatibleSaveHandler(ILogger<IncompatibleSaveHandler> logger) : IExceptionHandler
{
    public const string ProblemType = "save-incompatible";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not IncompatibleSaveException incompatible)
            return false;

        logger.LogWarning(incompatible.InnerException, "Save {Code} could not be loaded.", incompatible.Code);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = ProblemType,
                Title = "This save comes from an older version of Vaultrift."
            },
            cancellationToken);

        return true;
    }
}
