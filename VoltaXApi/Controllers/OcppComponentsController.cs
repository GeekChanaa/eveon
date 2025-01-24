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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OcppComponentsController
    {
        private readonly IOcppComponentsVariablesService _service;

        public OcppComponentsController(
            IOcppComponentsVariablesService service)
        {
            _service = service;
        }

        [HttpGet("GetComponents")]
        public async Task<ActionResult<List<OcppComponentListDto>>> GetComponents()
        {
            return await _service.GetComponents();
        }

        [HttpGet("GetVariables")]
        public async Task<ActionResult<List<OcppVariableListDto>>> GetVariables()
        {
            return await _service.GetVariables();
        }

        [HttpGet("GetVariableByName/{name}")]
        public async Task<ActionResult<OcppVariableInformationsDto>> GetVariableByName(string name)
        {
            return await _service.GetVariableByName(name);
        }

        [HttpGet("GetComponentByName/{name}")]
        public async Task<ActionResult<OcppComponentInformationsDto>> GetComponentByName(string name)
        {
            return await _service.GetComponentByName(name);
        }

        [HttpGet("GetComponentInstances/{name}")]
        public async Task<ActionResult<List<string>>> GetComponentInstances(string name)
        {
            return await _service.GetComponentInstances(name);
        }

        [HttpGet("GetComponentVariables/{name}")]
        public async Task<ActionResult<List<OcppVariableListDto>>> GetComponentVariables(string name)
        {
            return await _service.GetComponentVariables(name);
        }

        [HttpGet("GetVariablesByComponentNameAndInstance/{name}/{instance}")]
        public async Task<ActionResult<List<OcppVariableListDto>>> GetVariablesByComponentNameAndInstance(string name, string? instance)
        {
            return await _service.GetVariablesByComponentNameAndInstance(name, instance);
        }   
    }
}