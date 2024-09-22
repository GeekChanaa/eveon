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
    public class ReportReplyController : GenericController<ReportReply>
    {
        private readonly IRepository<ReportReply> _repository;

        public ReportReplyController(IRepository<ReportReply> repository) : base(repository)
        {
            _repository = repository;
        }
    }
}