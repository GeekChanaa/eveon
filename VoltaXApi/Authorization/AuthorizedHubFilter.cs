using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Hubs;

namespace VoltaXApi.Authorization;

public sealed class AuthorizedHubFilter(AccessService accessService, VoltaXApiDbContext db, HubConnections connections) : IHubFilter
{
    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var access = await accessService.Resolve(context.Context.User!);
        if (access == null) { context.Context.Abort(); throw new HubException("Authentication required."); }
        connections.Add(access.User.ID, context.Context);
        await next(context);
    }
    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        connections.Remove(context.Context.ConnectionId);
        await next(context, exception);
    }
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext context, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var access = await accessService.Resolve(context.Context.User!);
        if (access == null) throw new HubException("Authentication required.");
        var method = context.HubMethodName;
        var allowed = false;
        if (context.Hub is ChargerHub)
        {
            if (method == "SendMessageToCharger") allowed = access.Can("OperateChargePoints");
            else if (method is "JoinChargerGroup" or "LeaveChargerGroup")
            {
                var chargerId = Convert.ToString(context.HubMethodArguments[0]);
                allowed = access.Can("ViewChargePoints") || access.User.Role.Name == "Partner" && access.User.PartnerID != null &&
                    await db.ChargePoints.AnyAsync(p => p.ChargePointId == chargerId && !p.IsDeleted && p.ChargingStation.PartnerID == access.User.PartnerID);
            }
        }
        else if (context.Hub is ChargingSessionHub)
        {
            var id = Convert.ToInt32(context.HubMethodArguments[0]);
            if (method is "JoinSession" or "LeaveSession")
                allowed = access.Can("ViewChargingSessions") || await db.ChargingSessions.AnyAsync(s => s.ID == id && s.UserID == access.User.ID && !s.IsDeleted);
            if (method is "JoinUserSessions" or "LeaveUserSessions") allowed = id == access.User.ID || access.Can("ViewChargingSessions");
        }
        if (!allowed) throw new HubException("You do not have access to this resource.");
        return await next(context);
    }
}
