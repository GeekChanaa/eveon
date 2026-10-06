using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Services.Audit;

namespace VoltaXApi.Authorization;

public sealed class DashboardAccessFilter(AccessService accessService, VoltaXApiDbContext db, HubConnections connections, IAuditLogger audit) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var controller = descriptor.ControllerName;
        var action = descriptor.MethodInfo.Name;
        var method = context.HttpContext.Request.Method;
        if (descriptor.ControllerTypeInfo.Namespace == "VoltaXApi.Controllers" && EndpointPermissions.IsPublic(controller, action, method)) { await next(); return; }
        // OCPI roaming partners authenticate with their OCPI token (OcpiAuthFilter), never a user JWT.
        if (descriptor.ControllerTypeInfo.Namespace == "VoltaXApi.Ocpi.Controllers") { await next(); return; }
        // Chargers upload GetLog / GetDiagnostics files without a JWT: the one-time ticket token in the URL authorizes that single upload.
        if (descriptor.ControllerTypeInfo == typeof(VoltaXApi.OCPP.Controllers.LogUploadController) && action == nameof(VoltaXApi.OCPP.Controllers.LogUploadController.UploadChargerLog)) { await next(); return; }
        var access = await accessService.Resolve(context.HttpContext.User);
        if (access == null) { context.Result = new UnauthorizedResult(); return; }
        context.HttpContext.Items[typeof(AccessSnapshot)] = access;
        var user = access.User;
        var allowed = false;
        var sessionId = IntArgument(context, "chargingSessionID");
        var orderId = IntArgument(context, "orderID", "id");

        // Raw user CRUD exposes credential material and mass assignment; use DTO endpoints instead.
        if (controller == "User" && new[] { "GetAll", "GetById", "Create", "Update" }.Contains(action))
        { context.Result = new ForbidResult(); return; }

        // Driver self-service start/stop: EVDriverService resolves the caller's own card and
        // only stops the caller's own transaction, so any signed-in user may call them.
        if (controller == "EVDriver" && action is "RequestStartTransactionMobile" or "RequestStopTransactionMobile")
            allowed = true;
        else if (descriptor.ControllerTypeInfo.Namespace == "VoltaXApi.OCPP.Controllers")
            allowed = access.IsAdmin || controller != "LogUpload" && access.Can("AccessDashboard") && access.Can("OperateChargePoints");
        else if (controller == "Mobile") allowed = method == "GET" && action is "Wallet" or "Orders" or "NotificationPreferences"
            || method == "PUT" && action == "SaveNotificationPreference";
        else if (controller == "Access") allowed = action is "Me" or "GetProfile" or "UpdateProfile" || access.IsAdmin;
        else if (controller is "Role" or "Permission" or "Administrator") allowed = access.IsAdmin || controller == "Role" && action == "GetAllRoles" && access.Can("CreateUsers");
        else if (controller == "Auth")
        {
            allowed = new[] { "ValidateCurrentToken", "LogoutEverywhere", "CreateGoogleLinkTicket", "GoogleLinkWithToken", "GoogleUnlink", "GetLinkedAccounts", "ResendVerification", "GetTwoFactorStatus", "BeginTwoFactorEnrollment", "ConfirmTwoFactorEnrollment", "DisableTwoFactor" }.Contains(action);
            if (action == "ChangePassword") allowed = IntArgument(context, "UserID", "ID") == user.ID;
            if (new[] { "VerifyEmail", "VerifyPhone", "SendPhoneVerificationSms" }.Contains(action))
                allowed = string.Equals(StringArgument(context, "Email"), user.Email, StringComparison.OrdinalIgnoreCase);
            if (action == "SendEmailVerificationCode") allowed = IntArgument(context, "userID") == user.ID;
            if (action == "SendSmsTest") allowed = access.IsAdmin;
        }
        else if (controller == "User" && new[] { "GetUserInformations", "GetUserDashboardDisplayInformations", "GetUserPhoneNumber", "GetUserDebitCards", "UploadUserAvatar" }.Contains(action))
            allowed = IntArgument(context, "userID") == user.ID || access.Can(method == "GET" ? "ViewUsers" : "EditUsers");
        else if (controller == "User" && action is "UpdateUserEmail" or "UpdateUserPhone")
            allowed = IntArgument(context, "ID") == user.ID || access.Can("EditUsers");
        else if (controller == "NotificationSetting" && action is "GetUserNotificationSettings" or "SaveUserNotificationSetting")
            allowed = IntArgument(context, "UserID") == user.ID;
        else if (controller == "DebitCard")
        {
            var id = IntArgument(context, "id");
            // AddCard always saves a provider token for the caller; Delete only the caller's own card.
            allowed = action == "AddCard"
                || action == "Delete" && await db.DebitCards.AnyAsync(c => c.ID == id && c.UserID == user.ID && !c.IsDeleted);
        }
        else if (controller == "ChargingSession" && action is "GetChargingSessionInformations" or "GetChargingSessionInvoice")
            allowed = access.Can("ViewChargingSessions") || await db.ChargingSessions.AnyAsync(c => c.ID == sessionId && c.UserID == user.ID && !c.IsDeleted);
        else if (controller == "Order" && action is "GetInvoiceInfo" or "GetOrderInvoice" or "GetOrder")
            allowed = access.Can("ViewRechargeOrders") || await db.Orders.AnyAsync(o => o.ID == orderId && o.Card!.UserID == user.ID && !o.IsDeleted);
        else if (controller == "Partner" && action == "GetPartnerImageByID")
            allowed = access.Can("ViewPartners") || user.Role.Name == "Partner" && user.PartnerID != null && IntArgument(context, "partnerID") == user.PartnerID;
        else if (controller == "NotificationType" && method == "GET") allowed = true;
        // Station search is part of the driver experience: any signed-in user may use it.
        else if (controller == "ChargingStation" && action == "SearchChargingStations" && method == "GET") allowed = true;
        // Queries inside NotificationController are scoped to the caller's own notifications.
        else if (controller == "Notification" && action is "GetMyNotifications" or "MarkAsRead" or "MarkAllAsRead" or "DeleteMyNotification" or "DeleteAllMyNotifications") allowed = true;
        else if (controller == "FileManagement" && action == "UploadProfilePicture") allowed = true;
        // GDPR self-service (api/me): every action is scoped to the caller inside MeController.
        else if (controller == "Me") allowed = true;
        else if (controller == "ChargingSession" && action is "GetMyCurrentChargingSession" or "GetMyChargingSessions") allowed = true;
        else if (new[] { "ChargingSession.GetUserChargingSessions", "Card.GetUserRechargeCardsAsync", "Order.GetUserOrders", "UserInfoDownloadRequest.UserLastRequest", "UserInfoDownloadRequest.CreateDownloadRequest" }.Contains(controller + "." + action))
            allowed = IntArgument(context, "userID") == user.ID || access.Can(EndpointPermissions.Required(controller, action, method) ?? "__denied");
        else if (controller.StartsWith("Partner") && controller != "Partner")
        {
            // Only explicitly partner-scoped actions; inherited generic CRUD is not scoped.
            allowed = access.IsAdmin || method == "GET" && action.StartsWith("Get") && action is not "GetAll" and not "GetById" &&
                user.Role.Name == "Partner" && user.PartnerID != null && IntArgument(context, "partnerID") == user.PartnerID;
        }
        else if (method == "GET" && new[] { "ChargePointBrand.GetAllChargePointBrands", "ChargePointModel.GetAllChargePointModels", "ElectricVehicleModel.GetAllElectricVehicleModelsForSelect" }.Contains(controller + "." + action))
            allowed = access.Can("AccessDashboard") || controller == "ElectricVehicleModel";
        else
        {
            var required = EndpointPermissions.Required(controller, action, method);
            allowed = access.IsAdmin || required != null && access.Can("AccessDashboard") && access.Can(required);
        }

        // Shared read endpoints used by the partner portal must verify ownership before returning data.
        if (!allowed && method == "GET" && user.Role.Name == "Partner" && user.PartnerID != null)
        {
            var partnerId = user.PartnerID.Value;
            var pointId = IntArgument(context, "chargePointID", "id");
            var stationId = IntArgument(context, "chargingStationID", "id");
            var connectorId = IntArgument(context, "connectorID", "id");
            var externalId = StringArgument(context, "chargePointID");
            if (new[] { "ChargePoint.GetChargePointByID", "ChargePoint.GetById", "ChargePoint.GetChargePointConnectors", "Connector.GetChargePointConnectors", "ChargePointUptime.GetChargePointUptimes", "ChargingSession.GetChargePointChargingSessions", "ChargingSession.GetChargePointNbrChargingSessions", "ChargingSession.GetChargePointNbrChargingSessionsToday", "ChargingSession.GetChargePointNbrChargingSessionsLast30Days", "Transaction.GetChargePointTransactions", "Rating.GetChargePointRatings", "Statistics.GetChargePointStatisticsSummary" }.Contains(controller + "." + action))
                allowed = await db.ChargePoints.AnyAsync(p => p.ID == pointId && !p.IsDeleted && p.ChargingStation.PartnerID == partnerId);
            if (new[] { "ChargePointRealTime.Status", "MessageLog.GetPartnerMessageLogs" }.Contains(controller + "." + action))
                allowed = await db.ChargePoints.AnyAsync(p => p.ChargePointId == externalId && !p.IsDeleted && p.ChargingStation.PartnerID == partnerId);
            if (new[] { "ChargingStation.GetById", "ChargingStation.GetChargingStationForDisplay", "ChargingStationImage.GetChargingStationImages", "ChargingStationImage.GetChargingStationDisplayImage", "ChargePoint.GetChargingStationChargePoints", "StationLoadBalancing.GetStationLoadLimit", "StationLoadBalancing.GetStationAllocations", "StationLoadBalancing.GetStationAllocationHistory" }.Contains(controller + "." + action))
                allowed = await db.ChargingStations.AnyAsync(p => p.ID == stationId && !p.IsDeleted && p.PartnerID == partnerId);
            if (controller == "ConnectorUptime" && action == "GetConnectorUptimes")
                allowed = await db.Connectors.AnyAsync(c => c.ID == connectorId && !c.IsDeleted && c.ChargePoint.ChargingStation.PartnerID == partnerId);
            if (new[] { "ChargingStation", "ChargePoint", "Transaction", "ConnectorStatus" }.Contains(controller) && action.StartsWith("GetPartner"))
                allowed = IntArgument(context, "partnerID") == partnerId;
        }

        // Role assignment has one audited endpoint, never a side effect of editing personal data.
        if (allowed && controller == "User" && action == "EditUserDashboardInformations")
        {
            var targetId = IntArgument(context, "userID");
            var target = await db.Users.AsNoTracking().Include(u => u.Role).SingleOrDefaultAsync(u => u.ID == targetId);
            var dto = context.ActionArguments.Values.OfType<UserDashboardEditInformationsDto>().SingleOrDefault();
            allowed = target != null && dto != null && dto.RoleID == target.RoleID;
            if (allowed && !access.IsAdmin)
                allowed = target!.Role.Name != "Admin" && target.PartnerID == dto!.PartnerID &&
                    target.IsEmailVerified == dto.IsEmailVerified && target.IsPhoneNumberVerified == dto.IsPhoneNumberVerified;
            // Admin accounts cannot be suspended through generic user edits (avoids last-admin races).
            if (allowed && target!.Role.Name == "Admin") allowed = dto!.SuspendedAt == target.SuspendedAt;
        }
        if (allowed && controller == "User" && action == "CreateUserDashboard")
        {
            var dto = context.ActionArguments.Values.OfType<UserDashboardCreateDto>().Single();
            var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.ID == dto.RoleID && !r.IsDeleted);
            allowed = role != null && (access.IsAdmin || role.Name == "Customer" && dto.PartnerID == null && !dto.IsEmailVerified && !dto.IsPhoneNumberVerified);
        }
        if (allowed && controller == "User" && action == "Delete")
        {
            var targetId = IntArgument(context, "id");
            // Demote via the protected role endpoint before deleting an administrator.
            allowed = targetId != user.ID && !await db.Users.AnyAsync(u => u.ID == targetId && u.Role.Name == "Admin");
        }
        // Partners manage the load limits of their own stations (the station list is filtered to them in the controller).
        if (!allowed && controller == "StationLoadBalancing" && user.Role.Name == "Partner" && user.PartnerID != null)
        {
            var stationId = IntArgument(context, "chargingStationID");
            allowed = action == "GetLoadBalancingStations" && method == "GET"
                || action is "SaveStationLoadLimit" or "RebalanceStation" && await db.ChargingStations.AnyAsync(s => s.ID == stationId && !s.IsDeleted && s.PartnerID == user.PartnerID);
        }
        if (!allowed) { context.Result = new ForbidResult(); return; }
        var result = await next();
        // Every command sent to a charger (reset, unlock, remote start/stop, config...) is on record.
        if (result.Exception == null && method == "POST" && descriptor.ControllerTypeInfo.Namespace == "VoltaXApi.OCPP.Controllers")
            await audit.LogAsync($"Ocpp.{controller}.{action}", "ChargePoint", StringArgument(context, "chargePointID"));
        if (result.Exception == null && controller == "User" && action is "EditUserDashboardInformations" or "Delete")
        {
            var targetId = IntArgument(context, "userID", "id");
            if (targetId.HasValue) connections.Revoke(new[] { targetId.Value });
        }
    }

    private static string? StringArgument(ActionExecutingContext context, params string[] names)
    {
        foreach (var name in names)
        {
            var direct = context.ActionArguments.FirstOrDefault(pair => pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (direct.Value != null) return Convert.ToString(direct.Value);
            foreach (var argument in context.ActionArguments.Values.Where(v => v != null))
            {
                var property = argument!.GetType().GetProperties().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (property != null) return Convert.ToString(property.GetValue(argument));
            }
        }
        return null;
    }
    private static int? IntArgument(ActionExecutingContext context, params string[] names) =>
        int.TryParse(StringArgument(context, names), out var id) ? id : null;
}
