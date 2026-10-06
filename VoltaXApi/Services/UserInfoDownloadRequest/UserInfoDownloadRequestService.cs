using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Factories;

namespace VoltaXApi.Services
{
  public class UserInfoDownloadRequestService : IUserInfoDownloadRequestService
  {
    private readonly IUserInfoDownloadRequestRepository _userInfoDownloadRequestRepository;
    private readonly IMailRequestFactory _mailRequestFactory;
    private readonly IMailService _mailService;

    public UserInfoDownloadRequestService(
      IUserInfoDownloadRequestRepository userInfoDownloadRequestRepository,
      IMailService mailService,
      IMailRequestFactory mailRequestFactory
    )
    {
      _userInfoDownloadRequestRepository = userInfoDownloadRequestRepository;
      _mailService = mailService;
      _mailRequestFactory = mailRequestFactory;
    }

    public async Task<bool> DenyRequest(int requestID)
    {
      var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID)
        ?? throw new KeyNotFoundException("Request not found.");
      if (request.Status != DownloadRequestStatusEnum.Pending)
        throw new InvalidOperationException("Only pending requests can be denied.");

      await this._userInfoDownloadRequestRepository.DenyRequest(requestID);
      MailRequest mailRequest = this._mailRequestFactory.CreateDeniedDownloadInfoRequest(request.Email);

      await _mailService.SendDownloadInfoRequestDenied(mailRequest, System.Net.WebUtility.HtmlEncode(request.UserName));
      return true;
    }

    // Approval only queues the export: GdprExportWorker builds the archive and emails the link.
    public async Task<bool> ApproveRequest(int requestID)
    {
      var request = await _userInfoDownloadRequestRepository.GetRequestByID(requestID)
        ?? throw new KeyNotFoundException("Request not found.");
      if (request.Status != DownloadRequestStatusEnum.Pending)
        throw new InvalidOperationException("Only pending requests can be approved.");

      await this._userInfoDownloadRequestRepository.ApproveRequest(requestID);
      return true;
    }
  }
}
