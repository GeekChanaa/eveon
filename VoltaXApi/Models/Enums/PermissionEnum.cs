
namespace VoltaXApi.Models;

public enum PermissionEnum
{
    // Generic CRUD permissions
    ViewChargingStations,
    CreateChargingStations,
    EditChargingStations,
    DeleteChargingStations,

    ViewChargePoints,
    CreateChargePoints,
    EditChargePoints,
    DeleteChargePoints,

    ViewChargePointConfiguration,
    EditChargePointConfiguration,

    ViewChargingCards,
    CreateChargingCards,
    EditChargingCards,
    DeleteChargingCards,
    ViewChargingCardHistory,

    ViewNotices,
    CreateNotices,
    EditNotices,
    DeleteNotices,

    ViewReports,
    CreateReports,
    EditReports,
    DeleteReports,

    ViewUserInfoDownloadRequests,
    CreateUserInfoDownloadRequests,
    EditUserInfoDownloadRequests,
    DeleteUserInfoDownloadRequests,

    ViewChargingSessions,
    CreateChargingSessions,
    EditChargingSessions,
    DeleteChargingSessions,

    ViewTransactions,
    CreateTransactions,
    EditTransactions,
    DeleteTransactions,

    ViewRechargeOrders,
    CreateRechargeOrders,
    EditRechargeOrders,
    DeleteRechargeOrders,

    ViewSystemReports,
    CreateSystemReports,
    EditSystemReports,
    DeleteSystemReports,

    ViewUsers,
    CreateUsers,
    EditUsers,
    DeleteUsers,

    ViewPartners,
    CreatePartners,
    EditPartners,
    DeletePartners,

    AccessDashboard,
    ViewDashboard,
    ViewDocumentation,
    OperateChargePoints,
    ViewGlobalConfigurations,
    EditGlobalConfigurations,
    ViewComments,
    CreateComments,
    EditComments,
    DeleteComments,
    ViewOcppLocalLists,
    CreateOcppLocalLists,
    EditOcppLocalLists,
    DeleteOcppLocalLists,
    ViewStatisticsPage
}
