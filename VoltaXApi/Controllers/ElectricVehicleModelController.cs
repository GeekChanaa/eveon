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
    public class ElectricVehicleModelController : GenericController<ElectricVehicleModel>
    {
        private readonly IElectricVehicleModelRepository _repository;

        public ElectricVehicleModelController(IElectricVehicleModelRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllElectricVehicleModelsForSelect")]
        public async Task<List<EVModelForSelecttDto>> GetAllElectricVehicleModelsForSelect()
        {
            return await this._repository.GetAllElectricVehicleModelsForSelect();
        }

    }
}