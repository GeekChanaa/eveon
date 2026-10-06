using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Services;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInfoDownloadRequestController : GenericController<UserInfoDownloadRequest>
    {
        private readonly IUserInfoDownloadRequestRepository _repository;
        private readonly IUserInfoDownloadRequestService _service;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepository;
        
        public UserInfoDownloadRequestController(
            IUserInfoDownloadRequestRepository repository,
            IUserInfoDownloadRequestService service,
            INotificationService notificationService,
            IUserRepository userRepository) : base(repository)
        {
            _repository = repository;
            _service = service;
            _notificationService = notificationService;
            _userRepository = userRepository;
        }
        
        [HttpPost("create")]
        public async Task<ActionResult<UserInfoDownloadRequest>> CreateDownloadRequest([FromBody] int userId)
        {
            try
            {
                var request = await _repository.CreateDownloadRequestAsync(userId);
                var email = await _userRepository.GetUserEmailByID(userId);
                await _notificationService.NotifyDashboardAsync(
                    new DashboardNotification("Data Request", "UserInfoDownloadRequested", $"{email} requested a copy of their personal data.", request.ID.ToString()),
                    "ViewUserInfoDownloadRequests", $"/dashboard/user-info-download-requests/{request.ID}");
                return CreatedAtAction(nameof(GetById), new { id = request.ID }, new { request.ID, request.RequestTime, request.Status });
            }
            catch (ArgumentException)
            {
                return Conflict("A data export request is already in progress.");
            }
        }

        [HttpGet("getRequestByID/{requestID}")]
        public async Task<ActionResult<UserInfoDownloadRequestDisplayDto>> getRequestByID(int requestID)
        {
            var request = await _repository.GetRequestByID(requestID);
            if (request == null) return NotFound();
            return Ok(request);
        }

        [HttpGet("GetAllRequests")]
        public async Task<ActionResult<PagedList<UserInfoDownloadRequestListDto>>> GetAllRequests([FromQuery] GlobalParams globalParams)
        {
            var requests = this._repository.GetUserInfoDownloadRequestList(globalParams);
            var requestsList = await PagedList<UserInfoDownloadRequestListDto>.CreateAsync(requests,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(requestsList.CurrentPage, requestsList.PageSize, requestsList.TotalCount, requestsList.TotalPages);
            return requestsList;
        }

        [HttpGet("UserLastRequest/{userID}")]
        public async Task<ActionResult<UserInfoDownloadRequestDisplayDto>> UserLastRequest(int userID)
        {
            return await this._repository.UserLastRequest(userID);
        }

        [HttpPost("ApproveRequest/{requestID}")]
        public async Task<ActionResult<bool>> ApproveRequest(int requestID)
        {
            try
            {
                return await this._service.ApproveRequest(requestID);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException)
            {
                return Conflict("Only pending requests can be approved.");
            }
        }

        [HttpPost("DenyRequest/{requestID}")]
        public async Task<ActionResult<bool>> DenyRequest(int requestID)
        {
            try
            {
                return await this._service.DenyRequest(requestID);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException)
            {
                return Conflict("Only pending requests can be denied.");
            }
        }
    }
}