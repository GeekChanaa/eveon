using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Services;
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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : GenericController<Rating>
    {
        private readonly IRatingRepository _repository;

        public RatingController(IRatingRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // Get ChargePoint Ratings
        [HttpGet("GetChargePointRatings/{chargePointID}")]
        public async Task<IActionResult> GetChargePointRatings(int chargePointID)
        {
            var ratings = await _repository.GetChargePointRatings(chargePointID);
            return Ok(ratings);
        }
    }
}