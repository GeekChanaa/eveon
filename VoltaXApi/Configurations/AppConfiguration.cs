using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using OCPP.Core.Server;
using VoltaXApi.Hubs;

namespace VoltaXApi.Configurations;

public static class AppConfiguration
{
    public static void ConfigureSwagger(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
    
    public static void ConfigureRouting(WebApplication app)
    {
        app.UseRouting();
    }
    
    public static void ConfigureLocalization(WebApplication app)
    {
        app.UseRequestLocalization();
    }
    
    public static void ConfigureCors(WebApplication app)
    {
        app.UseCors("CorsPolicy");
    }
    
    public static void ConfigureCookiePolicy(WebApplication app)
    {
        app.UseCookiePolicy();
    }
    
    public static void ConfigureAuth(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }
    
    public static void ConfigureEndpoints(WebApplication app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "OCPP",
                pattern: "ocpp/{controller=OCPP}/{action=Index}/{id?}");
            endpoints.MapHub<ChargerHub>("/chargerHub");
            endpoints.MapHub<ChargingSessionHub>("/chargingSessionHub");
        });
    }
    
    public static void ConfigureStaticFiles(WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
            RequestPath = "/StaticFiles",
        });
    }
    
    public static void ConfigureWebSockets(WebApplication app)
    {
        var webSocketOptions = new WebSocketOptions()
        {
            ReceiveBufferSize = 8 * 1024
        };
        
        app.UseWebSockets(webSocketOptions);
    }
    
    public static void ConfigureOCPPMiddleware(WebApplication app)
    {
        app.UseOCPPMiddleware();
    }
}