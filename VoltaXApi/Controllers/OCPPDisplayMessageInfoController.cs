using VoltaXApi.Models;
using VoltaXApi.Dtos;
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
using VoltaXApi.Services;
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OCPPDisplayMessageInfoController : GenericController<OCPPDisplayMessageInfo>
    {
        private readonly IOCPPDisplayMessageInfoRepository _repository;

        public OCPPDisplayMessageInfoController(
            IOCPPDisplayMessageInfoRepository repository) : base(repository)
        {
            _repository = repository;
        }
    }
}