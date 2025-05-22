using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
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
using AutoMapper;
using AutoMapper.QueryableExtensions;
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class NoticeController : GenericController<Notice>
    {
        private readonly INoticeRepository _repository;
        private readonly INoticeService _noticeService;

        public NoticeController(
            INoticeRepository repository,
            INoticeService noticeService
            ) : base(repository)
        {
            _repository = repository;
            _noticeService = noticeService;
        }

        [HttpGet("GetNotices")]
        public async Task<List<NoticeListDto>> GetNotices([FromQuery] GlobalParams globalParams)
        {
            var notices = this._repository.GetNotices(globalParams);
            var list = await notices.ToListAsync();
            var noticesList = await PagedList<NoticeListDto>.CreateAsync(notices,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(noticesList.CurrentPage, noticesList.PageSize, noticesList.TotalCount, noticesList.TotalPages);
            return noticesList;
        }

        [HttpPost("CreateNotice")]
        public async Task<IActionResult> CreateNotice([FromForm] CreateNoticeDto notice)
        {
            if(Request.Form.Files.Where(f => f.Name.Contains("emailTemplate")).Count() > 0){
                var noticeEmailTemplate = Request.Form.Files.Where(f => f.Name.Contains("emailTemplate"))?.First();
                notice.EmailTemplate = noticeEmailTemplate;
            }
            
            await _noticeService.CreateNotice(notice);
            return StatusCode(200);
        }

        [HttpGet("GetNoticeByID/{noticeID}")]
        public async Task<IActionResult> GetNoticeByID(int noticeID)
        {
            return Ok(await _repository.GetNoticeByID(noticeID));
        }
        

    }
}