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
    public class StateController : GenericController<State>
    {
        private readonly IStateRepository _repository;

        public StateController(IStateRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllStateNamesByCountry")]
        public async Task<ActionResult<List<string>>> GetAllStateNamesByCountry([FromQuery] int id)
        {
            return await _repository.GetAllStateNamesByCountry(id);
        }
    }
}