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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : GenericController<Comment>
    {
        private readonly ICommentRepository _repository;

        public CommentController(ICommentRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpPost("CreateComment")]
        public async Task<IActionResult> CreateComment(CreateCommentDto commentDto)
        {
            await this._repository.CreateComment(commentDto);
            return StatusCode(204);
        }

        [HttpGet("GetCommentByID/{commentID}")]
        public async Task<IActionResult> GetCommentByID(int commentID)
        {
            var comment = await this._repository.GetCommentByID(commentID);
            return Ok(comment);
        }
    }
}