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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : GenericController<Customer>
    {
        private readonly ICustomerRepository _repository;

        public CustomerController(ICustomerRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // Getting all customer names
        [HttpGet("GetAllCustomersNames")]
        public async Task<ActionResult<List<CustomerNameDto>>> GettAllCustomersNames()
        {
            return await _repository.GetAllCustomersNames();
        }

        // Getting All customer names by name
        [HttpGet("GetAllCustomersNamesByName")]
        public async Task<ActionResult<List<CustomerNameDto>>> GetAllCustomersNamesByName([FromQuery] string name)
        {
            return await _repository.GetAllCustomersNamesByName(name);
        }
    }
}