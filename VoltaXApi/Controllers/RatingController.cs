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
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : GenericController<Rating>
    {
        private readonly IRatingRepository _repository;
        private readonly IMapper _mapper;

        public RatingController(
            IRatingRepository repository,
            IMapper mapper) : base(repository)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Get ChargePoint Ratings
        [HttpGet("GetChargePointRatings/{chargePointID}")]
        public async Task<IActionResult> GetChargePointRatings(int chargePointID, [FromQuery] GlobalParams globalParams)
        {
            var ratingsDto = (await _repository.GetAllAsync(globalParams)).Include(u => u.User).Where(u => u.EntityID == chargePointID).Where(u => u.Entity == "ChargePoint").ProjectTo<RatingListDto>(_mapper.ConfigurationProvider);
            var ratings = await PagedList<RatingListDto>.CreateAsync(ratingsDto, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(ratings.CurrentPage, ratings.PageSize, ratings.TotalCount, ratings.TotalPages);
            return Ok(ratings);
        }
    }
}