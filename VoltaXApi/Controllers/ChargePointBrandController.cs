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
    public class ChargePointBrandController : GenericController<ChargePointBrand>
    {
        private readonly IChargePointBrandRepository _repository;

        public ChargePointBrandController(IChargePointBrandRepository repository, IMapper mapper) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllChargePointBrands")]
        public async Task<IActionResult> GetAllChargePointBrands()
        {
            return Ok(await _repository.GetAllChargePointBrands());
        }
    }
}