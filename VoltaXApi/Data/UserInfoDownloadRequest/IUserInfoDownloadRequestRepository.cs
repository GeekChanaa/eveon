using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface IUserInfoDownloadRequestRepository : IRepository<UserInfoDownloadRequest>
    {
        Task<UserInfoDownloadRequest> CreateDownloadRequestAsync(int userID);
        Task<UserInfoDownloadRequest> ApproveDownloadRequestAsync(int requestId);
        Task<UserInfoDownloadRequest> DenyDownloadRequestAsync(int requestId);
        Task<UserInfoDownloadRequestDisplayDto> GetRequestByID(int requestId);
        IQueryable<UserInfoDownloadRequestListDto> GetUserInfoDownloadRequestList(GlobalParams globalParams);
        Task<UserInfoDownloadRequestDisplayDto> UserLastRequest(int userID);
        Task<bool> ApproveRequest(int requestID);
        Task<bool> DenyRequest(int requestID);

    }
}