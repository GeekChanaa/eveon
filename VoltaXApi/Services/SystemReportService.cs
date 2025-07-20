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
    public SystemReportService(
      ISystemReportRepository systemReportRepository,
      IMailService mailService,
      IUserRepository userRepository
    ) 
    {
      _systemReportRepo = systemReportRepository;
      _mailService = mailService;
      _userRepository = userRepository;
    }

    public async Task HandleReport(SystemReport report)
    {
      await this._systemReportRepo.AddAsync(report);
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