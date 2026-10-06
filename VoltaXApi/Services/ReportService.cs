using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public class ReportService : IReportService
  {
    private readonly IReportRepository _reportRepo;
    private readonly IMailService _mailService;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    public ReportService(
      IReportRepository ReportRepository,
      IMailService mailService,
      IUserRepository userRepository,
      INotificationService notificationService
    ) 
    {
      _reportRepo = ReportRepository;
      _mailService = mailService;
      _userRepository = userRepository;
      _notificationService = notificationService;
    }

    public async Task HandleReport(CreateReportDto report)
    {
      int reportID = await this._reportRepo.CreateReport(report);
      await _notificationService.NotifyDashboardAsync(
        new DashboardNotification("Report", "ReportCreated", $"New {report.ReportCategory} report from a user: {report.IssueDescription}", reportID.ToString()),
        "ViewReports", $"/dashboard/reports/{reportID}");
      if(report.IsEmail)
      {
        // await this._mailService.SendReportEmailToAdmin(report);
        await _mailService.SendReportEmailToSupport(reportID);
      }
    }

  }

}