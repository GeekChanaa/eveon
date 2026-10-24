using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using VoltaXApi.Exceptions;
using VoltaXApi.Filters;

namespace VoltaX.Tests.Api;

public class GlobalExceptionFilterTests
{
    private static ObjectResult Handle(Exception exception)
    {
        var filter = new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance,
            new HostingEnvironment { EnvironmentName = Environments.Production });
        var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(action, new List<IFilterMetadata>()) { Exception = exception };

        filter.OnException(context);

        return Assert.IsType<ObjectResult>(context.Result);
    }

    [Fact]
    public void A_remote_start_before_the_car_is_plugged_in_is_a_conflict_with_its_reason()
    {
        // The simulator end-to-end run found it answered 500 "An unexpected error occurred."
        var result = Handle(new NoEVConnectedException("No EV is currently connected to the charge point."));

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Contains("No EV is currently connected", result.Value!.GetType().GetProperty("error")!.GetValue(result.Value) as string);
    }

    [Fact]
    public void An_unexpected_exception_stays_a_500_without_its_message()
    {
        var result = Handle(new InvalidOperationException("SELECT * FROM secrets"));

        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("An unexpected error occurred.", result.Value!.GetType().GetProperty("error")!.GetValue(result.Value) as string);
    }
}
