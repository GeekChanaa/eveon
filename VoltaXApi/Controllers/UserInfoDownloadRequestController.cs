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
        
        public UserInfoDownloadRequestController(
            IUserInfoDownloadRequestRepository repository,
            IUserInfoDownloadRequestService service) : base(repository)
        {
            _repository = repository;
            _service = service;
        }
        
        [HttpPost("create")]
        public async Task<ActionResult<UserInfoDownloadRequest>> CreateDownloadRequest([FromBody] int userId)
        {
            try
            {
                var request = await _repository.CreateDownloadRequestAsync(userId);
                return CreatedAtAction(nameof(GetById), new { id = request.ID }, request);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet("getRequestByID/{requestID}")]
        public async Task<ActionResult<UserInfoDownloadRequestDisplayDto>> getRequestByID(int requestID)
        {
            try
            {
                var request = await _repository.GetRequestByID(requestID);
                return Ok(request);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet("GetAllRequests")]
        public async Task<ActionResult<PagedList<UserInfoDownloadRequestListDto>>> GetAllRequests([FromQuery] GlobalParams globalParams)
        {
            try
            {
                var requests = this._repository.GetUserInfoDownloadRequestList(globalParams);
                var requestsList = await PagedList<UserInfoDownloadRequestListDto>.CreateAsync(requests,globalParams.PageNumber, globalParams.PageSize);
                Response.AddPagination(requestsList.CurrentPage, requestsList.PageSize, requestsList.TotalCount, requestsList.TotalPages);
                return requestsList;
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet("UserLastRequest/{userID}")]
        public async Task<ActionResult<UserInfoDownloadRequestDisplayDto>> UserLastRequest(int userID)
        {
            try
            {
                return await this._repository.UserLastRequest(userID);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("ApproveRequest/{requestID}")]
        public async Task<ActionResult<bool>> ApproveRequest(int requestID)
        {
            try
            {
                return await this._service.ApproveRequest(requestID);
            }
            catch (Exception ex)
            {
                throw ex;
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("DenyRequest/{userID}")]
        public async Task<ActionResult<bool>> DenyRequest(int userID)
        {
            try
            {
                return await this._service.DenyRequest(userID);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        
    }
}