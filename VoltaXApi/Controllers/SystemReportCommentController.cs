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
using VoltaXApi.Authorization;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class SystemReportCommentController : GenericController<SystemReportComment>
    {

      private readonly ISystemReportCommentRepository _repository;

        public SystemReportCommentController(
          ISystemReportCommentRepository repository
        ) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetSystemReportComments/{systemReportID}")]
        public async Task<IActionResult> GetSystemReportComments(int systemReportID,[FromQuery] GlobalParams globalParams)
        {
            var systemReportComments = await PagedList<SystemReportCommentDisplayDto>.CreateAsync((_repository.GetSystemReportComments(systemReportID)), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(systemReportComments.CurrentPage, systemReportComments.PageSize, systemReportComments.TotalCount, systemReportComments.TotalPages);
            return Ok(systemReportComments);
        }

        /// <summary>Adds a comment to a system report, written by the caller.</summary>
        [HttpPost("CreateComment/{systemReportID}")]
        public async Task<IActionResult> CreateComment(int systemReportID, [FromBody] CreateSystemReportCommentDto comment,
            [FromServices] VoltaXApiDbContext db, CancellationToken ct)
        {
            if (!await db.SystemReports.AnyAsync(r => r.ID == systemReportID, ct))
                return NotFound();
            var row = new SystemReportComment { SystemReportID = systemReportID, Content = comment.Content.Trim(), UserID = this.User().ID };
            db.SystemReportComments.Add(row);
            await db.SaveChangesAsync(ct);
            return Ok(new { id = row.ID });
        }

        /// <summary>Comments are added with <see cref="CreateComment"/> (author taken from the session), never with a client-chosen author.</summary>
        public override Task<IActionResult> Create(SystemReportComment entity) => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));

        public override Task<IActionResult> Update(int id, [FromBody] System.Text.Json.JsonElement entityToUpdate) =>
            Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed));
    }

    public sealed class CreateSystemReportCommentDto
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = "";
    }
}