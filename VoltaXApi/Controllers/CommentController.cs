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
        private readonly IRepository<Comment> _repository;

        public CommentController(IRepository<Comment> repository) : base(repository)
        {
            _repository = repository;
        }

        // You can override the base methods or add specific methods for this controller
    }
}