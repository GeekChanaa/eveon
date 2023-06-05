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
    public class TransactionController : GenericController<Transaction>
    {
        private readonly ITransactionRepository _repository;

        public TransactionController(ITransactionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // You can override the base methods or add specific methods for this controller
        [HttpGet("countEnergy")]
        public async Task<IActionResult> CountEnergy()
        {
            double count = await _repository.CountEnergy(u => true);
            return Ok(count);
        }

        [HttpGet("countEnergyToday")]
        public async Task<IActionResult> CountEnergyToday()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            double count = await _repository.CountEnergy(u => u.StartTime >= today && u.StartTime < tomorrow);
            return Ok(count);
        }

        [HttpGet("countEnergyByDay")]
        public async Task<IActionResult> GetEnergyConsumptionByDay()
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-29);

            var energyConsumptionByDay = new List<double>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                DateTime currentDay = date.Date;
                DateTime nextDay = currentDay.AddDays(1);

                double energyConsumption = await _repository
                    .CountEnergy(u => u.StartTime >= currentDay && u.StartTime < nextDay);

                energyConsumptionByDay.Add(energyConsumption);
            }

            return Ok(energyConsumptionByDay);
        }
    
    }
    
}