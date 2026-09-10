using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PerfectDraft.Product.Api.Upstreams;

namespace PerfectDraft.Product.Api.Infrastructure;

/// <summary>
/// Turns an unreadable upstream into a 503 with a ProblemDetails body.
/// </summary>
/// <remarks>
/// The response is deliberately vague. Exception types, stack traces and file paths are internal
/// details, and returning them tells a caller nothing useful while telling an attacker plenty. They
/// are logged instead. Anything that is not an upstream failure is left unhandled so it keeps its
/// normal treatment.
/// </remarks>
public sealed class UpstreamExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<UpstreamExceptionHandler> _logger;

    public UpstreamExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<UpstreamExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UpstreamUnavailableException upstreamFailure)
        {
            return false;
        }

        _logger.LogError(
            upstreamFailure,
            "Request failed because the {Upstream} upstream is unavailable.",
            upstreamFailure.Upstream);

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service unavailable",
                Detail = "A system this API depends on could not be read. Please try again later."
            }
        });
    }
}
