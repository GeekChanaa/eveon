using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public class SystemReportService : ISystemReportService
  {
    private readonly ISystemReportRepository _systemReportRepo;
    private readonly IMailService _mailService;
    public SystemReportService(
      ISystemReportRepository systemReportRepository,
      IMailService mailService
    )
    {
      _systemReportRepo = systemReportRepository;
      _mailService = mailService;
    }

    public async Task HandleReport(SystemReport report)
    {
      await this._systemReportRepo.AddAsync(report);
      if(report.IsEmail)
        await this._mailService.SendReportEmail(report);
      // Send the email as a notification for the assigned person also.
    }

  }

}