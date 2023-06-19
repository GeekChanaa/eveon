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
    public class CountryController : GenericController<Country>
    {
        private readonly ICountryRepository _repository;

        public CountryController(ICountryRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllCountryNames")]
        public async Task<ActionResult<List<CountryNameDto>>> GetAllCountryNames()
        {
            return await _repository.GetAllCountryNames();
        }
    }
}