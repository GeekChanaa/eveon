using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Server;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Services;


namespace VoltaXApi.Controllers;


[ApiController]
[Route("api/partners/{partnerId}/[controller]")]
public class PartnerChargePointController : GenericController<ChargePoint>
{
    private readonly IChargePointRepository _repository;

    public PartnerChargePointController(
        IChargePointRepository repository) : base(repository)
    {
        _repository = repository;
    }

    [HttpGet("GetAllPartnerChargePoints")]
    public async Task<List<ChargePointCRListDto>> GetAllPartnerChargePoints([FromQuery] GlobalParams globalParams, int partnerID)
    {
        var chargePoints = this._repository.GetAllPartnerChargePoints(globalParams, partnerID);
        var chargePointsList = await PagedList<ChargePointCRListDto>.CreateAsync(chargePoints,globalParams.PageNumber, globalParams.PageSize);
        Response.AddPagination(chargePointsList.CurrentPage, chargePointsList.PageSize, chargePointsList.TotalCount, chargePointsList.TotalPages);
        return chargePointsList;
    }
}
