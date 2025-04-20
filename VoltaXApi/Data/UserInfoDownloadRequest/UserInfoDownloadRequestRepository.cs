using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public class UserInfoDownloadRequestRepository : Repository<UserInfoDownloadRequest>,IUserInfoDownloadRequestRepository
    {
        public UserInfoDownloadRequestRepository(
            VoltaXApiDbContext context) : base(context)
        {
        }

        public async Task<UserInfoDownloadRequest> CreateDownloadRequestAsync(int userID)
        {
            var request = new UserInfoDownloadRequest
            {
                UserID = userID,
                RequestTime = DateTime.UtcNow,
                Status = DownloadRequestStatusEnum.Pending
            };
            
            // Add to database
            await _context.UserInfoDownloadRequests.AddAsync(request);
            await _context.SaveChangesAsync();
            
            return request;
        }
        
        public async Task<UserInfoDownloadRequest> ApproveDownloadRequestAsync(int requestId)
        {
            var request = await _context.UserInfoDownloadRequests
                .FirstOrDefaultAsync(r => r.ID == requestId && !r.IsDeleted);
                
            if (request == null)
            {
                return null;
            }
            
            if (request.Status != DownloadRequestStatusEnum.Pending)
            {
                throw new InvalidOperationException($"Cannot approve request with status: {request.Status}");
            }
            
            request.Status = DownloadRequestStatusEnum.Approved;
            
            // Save changes
            _context.UserInfoDownloadRequests.Update(request);
            await _context.SaveChangesAsync();
            
            return request;
        }

        public async Task<UserInfoDownloadRequest> DenyDownloadRequestAsync(int requestId)
        {
            var request = await _context.UserInfoDownloadRequests
                .FirstOrDefaultAsync(r => r.ID == requestId && !r.IsDeleted);
                
            if (request == null)
            {
                return null;
            }
            
            if (request.Status != DownloadRequestStatusEnum.Pending)
            {
                throw new InvalidOperationException($"Cannot deny request with status: {request.Status}");
            }
            
            request.Status = DownloadRequestStatusEnum.Denied;
            
            // Save changes
            _context.UserInfoDownloadRequests.Update(request);
            await _context.SaveChangesAsync();
            
            return request;
        }

        public IQueryable<UserInfoDownloadRequestListDto> GetUserInfoDownloadRequestList(GlobalParams globalParams)
        {
            return GetAllAsync(globalParams).Select(u  => new UserInfoDownloadRequestListDto{
                ID = u.ID,
                UserName = u.User.FullName,
                RequestTime = u.RequestTime,
                Status = u.Status,
            }).AsQueryable();
        }

        public async Task<UserInfoDownloadRequestDisplayDto> GetRequestByID(int requestId)
        {
            return await dbSet.Select(u  => new UserInfoDownloadRequestDisplayDto{
                ID = u.ID,
                UserName = u.User.FullName,
                Email = u.User.Email,
                RequestTime = u.RequestTime,
                Status = u.Status,
            }).FirstOrDefaultAsync(u => u.ID == requestId);
        }

        
        public async Task<UserInfoDownloadRequestDisplayDto> UserLastRequest(int userID)
        {
            return await dbSet.Where(u => u.UserID == userID).OrderByDescending(u => u.CreatedAt).Select(u  => new UserInfoDownloadRequestDisplayDto{
                ID = u.ID,
                UserName = u.User.FullName,
                Email = u.User.Email,
                RequestTime = u.RequestTime,
                Status = u.Status,
            }).FirstOrDefaultAsync();
        }

        public async Task<bool> ApproveRequest(int requestID)
        {
            var request = await dbSet.FirstOrDefaultAsync(u => u.ID == requestID);
            request.Status = DownloadRequestStatusEnum.Approved;
            await this.Update(request);
            return true;
        }
        
        public async Task<bool> DenyRequest(int requestID)
        {
            var request = await dbSet.FirstOrDefaultAsync(u => u.ID == requestID);
            request.Status = DownloadRequestStatusEnum.Denied;
            await this.Update(request);
            return true;
        }
    }
}

