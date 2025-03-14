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
using VoltaXApi.Services;
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OCPPConfigurationItemController : GenericController<OCPPConfigurationItem>
    {
        private readonly IOCPPConfigurationItemRepository _repository;

        public OCPPConfigurationItemController(
            IOCPPConfigurationItemRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargePointConfigurationItems/{chargePointID}")]
        public async Task<ActionResult<List<OCPPConfigurationItemListDto>>> GetChargePointConfigurationItems(int chargePointID, [FromQuery] GlobalParams globalParams)
        {
            var chargePoints = this._repository.GetChargePointConfigurationItems(chargePointID,globalParams);
            var chargePointsList = await PagedList<OCPPConfigurationItemListDto>.CreateAsync(chargePoints,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargePointsList.CurrentPage, chargePointsList.PageSize, chargePointsList.TotalCount, chargePointsList.TotalPages);
            return chargePointsList;
        }
    }
}