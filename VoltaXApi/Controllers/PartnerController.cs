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
    public class PartnerController : GenericController<Partner>
    {
        private readonly IPartnerRepository _repository;
        public PartnerController(IPartnerRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetPartners")]
        public async Task<List<PartnerListDto>> GetPartners([FromQuery] GlobalParams globalParams)
        {
            var partners = this._repository.GetPartners(globalParams);
            var partnersList = await PagedList<PartnerListDto>.CreateAsync(partners,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(partnersList.CurrentPage, partnersList.PageSize, partnersList.TotalCount, partnersList.TotalPages);
            return partnersList;
        }

        [HttpGet("GetPartnerByID/{partnerId}")]
        public async Task<PartnerDisplayDto> GetPartnerByID(int partnerId)
        {
            return await this._repository.GetPartnerByID(partnerId);
        }

        [HttpPost("CreatePartner")]
        public async Task CreatePartner(CreatePartnerDto partner)
        {
            await _repository.CreatePartner(partner);
        }
    }
}