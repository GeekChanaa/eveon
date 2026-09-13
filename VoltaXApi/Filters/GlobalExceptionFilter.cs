using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Filters;

/// <summary>
/// Turns an exception into a status code, and puts it on record.
///
/// The two halves are deliberately different: the log gets everything (message, stack,
/// route, correlation id), the response gets the message and — outside development —
/// no stack trace. A stack trace on the wire tells an attacker the file layout, the
/// package versions and often the SQL behind a failure.
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

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

        LogException(context, statusCode);

        context.Result = new ObjectResult(new
        {
            error = context.Exception.Message,
            // Only ever handed out on a developer machine.
            stackTrace = _environment.IsDevelopment() ? context.Exception.StackTrace : null
        })
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// A 4xx is an expected outcome of a bad request, so it stays at Warning; anything
    /// 5xx is a defect and goes out at Error, where the dedicated error log picks it up.
    /// </summary>
    private void LogException(ExceptionContext context, int statusCode)
    {
        string method = context.HttpContext.Request.Method;
        string path = context.HttpContext.Request.Path.Value ?? string.Empty;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                context.Exception,
                "Unhandled {ExceptionType} on {Method} {Path} answered with {StatusCode}",
                context.Exception.GetType().Name, method, path, statusCode);

            return;
        }

        _logger.LogWarning(
            "{ExceptionType} on {Method} {Path} answered with {StatusCode}: {Reason}",
            context.Exception.GetType().Name, method, path, statusCode, context.Exception.Message);
    }
}
