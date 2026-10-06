using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public class SystemReportService : ISystemReportService
  {
    private readonly ISystemReportRepository _systemReportRepo;
    private readonly IMailService _mailService;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    public SystemReportService(
      ISystemReportRepository systemReportRepository,
      IMailService mailService,
      IUserRepository userRepository,
      INotificationService notificationService
    ) 
    {
      _systemReportRepo = systemReportRepository;
      _mailService = mailService;
      _userRepository = userRepository;
      _notificationService = notificationService;
    }

    public async Task HandleReport(SystemReport report)
    {
      await this._systemReportRepo.AddAsync(report);
      if(report.IsNotification)
      {
        await _notificationService.NotifyDashboardAsync(
          new DashboardNotification("System Report", "SystemReportCreated",
            $"New {report.Criticality} system report: {report.IssueDescription}", report.ID.ToString(),
            Urgent: report.Criticality >= ReportCriticality.High),
          "ViewSystemReports", $"/dashboard/system-reports/{report.ID}");
      }
      if(report.IsEmail)
      {
        await _mailService.SendReportEmailToSupport(report.ID);
        await this._mailService.SendReportEmailToAdmin(report);
        if(report.AssignedID != null){
          string email = await _userRepository.GetUserEmailByID(report.AssignedID ?? 1);
        }
      }
    }

  }

}