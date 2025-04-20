using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface IUserInfoDownloadRequestService
    {
      Task<bool> DenyRequest(int requestID);
      Task<bool> ApproveRequest(int requestID);
    }
}