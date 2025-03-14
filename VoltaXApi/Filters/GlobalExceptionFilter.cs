
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,

            ValidationException => StatusCodes.Status400BadRequest,

            UnauthorizedException => StatusCodes.Status401Unauthorized,
            InvalidOcppRequestException => StatusCodes.Status400BadRequest,
            OcppProtocolException => StatusCodes.Status502BadGateway,
            ChargePointNotFoundException => StatusCodes.Status404NotFound,
            TransactionNotFoundException => StatusCodes.Status404NotFound,
            CardNotFoundException => StatusCodes.Status404NotFound,
            ConnectorUnavailableException => StatusCodes.Status409Conflict,
            AuthorizationFailedException => StatusCodes.Status401Unauthorized,
            RateLimitExceededException => StatusCodes.Status429TooManyRequests,
            OcppServerException => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status500InternalServerError
        };

        context.Result = new ObjectResult(new
        {
            error = context.Exception.Message,
            stackTrace = context.Exception.StackTrace
        })
        {
            StatusCode = statusCode
        };
    }
}