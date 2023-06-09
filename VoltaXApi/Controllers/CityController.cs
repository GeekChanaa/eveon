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
    public class CityController : GenericController<City>
    {
        private readonly ICityRepository _repository;

        public CityController(ICityRepository repository) : base(repository)
        {
            _repository = repository;
        }

        
        [HttpGet("GetAllCityNamesByCountry")]
        public async Task<ActionResult<List<string>>> GetAllCityNamesByCountry([FromQuery] int id)
        {
            return await _repository.GetAllCityNamesByCountry(id);
        }
    }
}