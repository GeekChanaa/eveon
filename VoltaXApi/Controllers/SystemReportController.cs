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

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemReportController : GenericController<SystemReport>
    {
        private readonly ISystemReportRepository _repository;

        public SystemReportController(ISystemReportRepository repository) : base(repository)
        {
            _repository = repository;
        }
    }
}
