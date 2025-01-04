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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController
    {
        private readonly GlobalConfigurations _globalConfigurations;
        public ConfigurationController(
          GlobalConfigurations globalConfigurations
        )
        {
          _globalConfigurations = globalConfigurations;
        }

        [HttpGet("GetGlobalConfigurations")]
        public async Task<ActionResult<GlobalConfigurations>> GetGlobalConfigurations()
        {
            return this._globalConfigurations;
        }

        
    }
}