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
    public class ChargePointModelController : GenericController<ChargePointModel>
    {
        private readonly IChargePointModelRepository _repository;

        public ChargePointModelController(IChargePointModelRepository repository, IMapper mapper) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllChargePointModels")]
        public async Task<IActionResult> GetAllChargePointModels()
        {
            return Ok(await _repository.GetAllChargePointModels());
        }
    }
}