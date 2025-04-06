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
    public ReportService(
      IReportRepository ReportRepository,
      IMailService mailService,
      IUserRepository userRepository
    ) 
    {
      _reportRepo = ReportRepository;
      _mailService = mailService;
      _userRepository = userRepository;
    }

    public async Task HandleReport(CreateReportDto report)
    {
      int reportID = await this._reportRepo.CreateReport(report);
      if(report.IsEmail)
      {
        // await this._mailService.SendReportEmailToAdmin(report);
        await _mailService.SendReportEmailToSupport(reportID);
      }
    }

  }

}