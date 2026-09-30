using CivicConnect.Core.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CivicConnect.Web.Infrastructure;

// Turns our exceptions into the problem+json errors described in Docs/API/API_Contract_v1.md.
public class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        ProblemDetails problem = ex switch
        {
            ValidationFailedException v => new ValidationProblemDetails(v.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = v.Message
            },
            UnauthenticatedException u => Plain(StatusCodes.Status401Unauthorized, u.Message),
            ForbiddenException f => Plain(StatusCodes.Status403Forbidden, f.Message),
            NotFoundException n => Plain(StatusCodes.Status404NotFound, n.Message),
            ConflictException c => Plain(StatusCodes.Status409Conflict, c.Message),
            _ => null!
        };

        if (problem is null)
        {
            // Full detail goes to the log; the caller only gets a generic message.
            log.LogError(ex, "Unhandled error on {Method} {Path}", http.Request.Method, http.Request.Path);
            problem = Plain(StatusCodes.Status500InternalServerError, "Something went wrong on our side.");
        }

        http.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
    }

    private static ProblemDetails Plain(int status, string title) => new() { Status = status, Title = title };
}
