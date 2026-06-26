using Microsoft.AspNetCore.Diagnostics;
using MovieCore.Exceptions;

namespace MovieApi.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
        await Results.Problem(detail: exception.Message, statusCode: status).ExecuteAsync(context);
        return true;
    }
}