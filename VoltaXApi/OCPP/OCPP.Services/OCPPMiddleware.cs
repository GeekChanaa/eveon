using VoltaXApi.OCPP.Handlers;

namespace OCPP.Core.Server
{
    public class OCPPMiddleware
    {
        private readonly RequestDelegate _next;

        public OCPPMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            // Scoped to this request: never stored on the (application-lifetime) middleware.
            var wsRequestsHandler = context.RequestServices.GetRequiredService<IWebSocketRequestsHandler>();
            await wsRequestsHandler.Handle(context);
        }
    }

    public static class OCPPMiddlewareExtensions
    {
        public static IApplicationBuilder UseOCPPMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OCPPMiddleware>();
        }
    }
}
