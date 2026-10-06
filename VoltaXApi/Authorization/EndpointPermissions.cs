namespace VoltaXApi.Authorization;

// Unmapped endpoints are admin-only. New routes cannot silently become public.
public static class EndpointPermissions
{
    public static readonly Dictionary<string, string> Resources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ChargingStation"] = "ChargingStations", ["ChargingStationImage"] = "ChargingStations",
        ["ChargePoint"] = "ChargePoints", ["Connector"] = "ChargePoints", ["ChargePointBrand"] = "ChargePoints",
        ["ChargePointModel"] = "ChargePoints", ["ChargeTag"] = "ChargePoints", ["ChargePointRealTime"] = "ChargePoints",
        ["ChargePointUptime"] = "ChargePoints", ["ConnectorUptime"] = "ChargePoints", ["ConnectorStatus"] = "ChargePoints",
        ["MessageLog"] = "ChargePointConfiguration", ["OCPPConfigurationItem"] = "ChargePointConfiguration",
        ["OcppComponents"] = "ChargePointConfiguration", ["OCPPDisplayMessageInfo"] = "ChargePointConfiguration",
        ["OcppDefaultConfiguration"] = "ChargePointConfiguration",
        ["OCPPLocalListItem"] = "OcppLocalLists", ["OCPPLocalListVersion"] = "OcppLocalLists",
        ["Card"] = "ChargingCards", ["CardChangeHistory"] = "ChargingCardHistory",
        ["Notice"] = "Notices", ["Report"] = "Reports", ["ReportReply"] = "Reports",
        ["SystemReport"] = "SystemReports", ["SystemReportComment"] = "SystemReports",
        ["UserInfoDownloadRequest"] = "UserInfoDownloadRequests", ["ChargingSession"] = "ChargingSessions",
        ["Transaction"] = "Transactions", ["Order"] = "RechargeOrders", ["User"] = "Users", ["Partner"] = "Partners",
        ["Comment"] = "Comments", ["CommentReply"] = "Comments", ["Rating"] = "Reports", ["RatingReport"] = "Reports",
        ["Configuration"] = "GlobalConfigurations", ["AccountDeletion"] = "Users",
        ["ChargerEvent"] = "ChargePoints", ["ChargerDisplayMessage"] = "ChargePoints",
        ["ChargingProfile"] = "ChargePoints", ["ChargingStrategy"] = "ChargePoints", ["StationLoadBalancing"] = "ChargingStations"
    };
    public static string? Required(string controller, string action, string method)
    {
        if (controller == "ChargePoint" && action == "SendMessageToChargePoint") return "OperateChargePoints";
        // Smart charging actions that send profiles to chargers.
        if (controller == "ChargingStrategy" && action == "ApplyChargingStrategy" || controller == "StationLoadBalancing" && action == "RebalanceStation") return "OperateChargePoints";
        // Data a charger returned about a customer (CustomerInformation): personal data, read-only.
        if (controller == "CustomerInformationReport") return method == "GET" ? "ViewUsers" : null;
        if (controller == "FileManagement" && action == "UploadChargingStationPicture") return "EditChargingStations";
        if (controller == "Statistics") return method == "GET" ? "ViewStatisticsPage" : null;
        if (!Resources.TryGetValue(controller, out var resource)) return null;
        // These GET endpoints perform a mutation and must never inherit read access.
        var operation = method == "GET" ? "View" : method == "DELETE" ? "Delete" :
            action.StartsWith("Create") || action == "Add" ? "Create" : "Edit";
        if (action.StartsWith("Approve") || action.StartsWith("Deny") || action.StartsWith("Reset") || action.StartsWith("Recharge")) operation = "Edit";
        var permission = operation + resource;
        return AccessService.Catalog.Contains(permission) ? permission : null;
    }
    public static bool IsPublic(string controller, string action, string method) =>
        controller == "PhoneLogin" && method == "POST" && new[] { "RequestCode", "VerifyCode" }.Contains(action) ||
        controller == "Mobile" && method == "GET" && new[] { "Station", "Reviews" }.Contains(action) ||
        controller == "Auth" && new[] { "Register", "Login", "Refresh", "Logout", "ResetPassword", "ResetPasswordRequest", "ResetPasswordRequestForMobile", "VerifyResetPasswordCodeForMobile", "RequestPasswordReset", "VerifyTwoFactor", "GoogleLogin", "GoogleLink", "GoogleCallback", "GoogleTokenLogin" }.Contains(action) ||
        controller == "PartnerAuth" && new[] { "RequestPasswordReset", "ResetPassword", "Login", "GoogleLogin", "GoogleTokenLogin" }.Contains(action) ||
        method == "GET" && new[] { "User.UserEmailExists", "User.UserPhoneExists", "User.IsEmailUnique", "User.IsPhoneUnique", "Country.GetAllCountryNames", "State.GetAllStateNamesByCountry", "City.GetAllCityNamesByState", "City.GetAllMoroccoCityNames", "ChargingStation.GetChargingStationsForMap", "ChargePoint.GetChargePointByQrCode", "ElectricVehicleModel.GetAllElectricVehicleModelsForSelect" }.Contains(controller + "." + action);
}
