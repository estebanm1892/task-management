using Microsoft.AspNetCore.Mvc;
using TaskManagement.Core.Tasks;
using TaskManagement.Core.Users;

namespace TaskManagement.Api.Middleware;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var (status, detail) = MapException(exception);
            if (status == StatusCodes.Status503ServiceUnavailable)
            {
                logger.LogError(exception, "Persistence failure while processing the request.");
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = status,
                Title = status == StatusCodes.Status503ServiceUnavailable
                    ? "No fue posible completar la operación."
                    : "No fue posible aplicar la solicitud.",
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static (int Status, string Detail) MapException(Exception exception) => exception switch
    {
        UserValidationException or TaskValidationException =>
            (StatusCodes.Status400BadRequest, exception.Message),
        DuplicateUserEmailException =>
            (StatusCodes.Status409Conflict, exception.Message),
        UserResourceNotFoundException =>
            (StatusCodes.Status404NotFound, exception.Message),
        TaskResourceNotFoundException =>
            (StatusCodes.Status404NotFound, exception.Message),
        TaskTransitionException transition when transition.Message.Contains("no es válido", StringComparison.OrdinalIgnoreCase) =>
            (StatusCodes.Status400BadRequest, transition.Message),
        TaskTransitionException =>
            (StatusCodes.Status409Conflict, exception.Message),
        UserPersistenceException or TaskPersistenceException =>
            (StatusCodes.Status503ServiceUnavailable, "El servicio no está disponible temporalmente. Intente nuevamente más tarde."),
        _ =>
            (StatusCodes.Status503ServiceUnavailable, "El servicio no está disponible temporalmente. Intente nuevamente más tarde.")
    };
}
