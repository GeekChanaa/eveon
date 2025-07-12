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
        private readonly IPartnerService _partnerService;
        public PartnerController(
            IPartnerRepository repository,
            IPartnerService partnerService) : base(repository)
        {
            _repository = repository;
            _partnerService = partnerService;
        }

        [HttpGet("GetPartners")]
        public async Task<List<PartnerListDto>> GetPartners([FromQuery] GlobalParams globalParams)
        {
            var partners = this._repository.GetPartners(globalParams);
            var list = await partners.ToListAsync();
            var partnersList = await PagedList<PartnerListDto>.CreateAsync(partners, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(partnersList.CurrentPage, partnersList.PageSize, partnersList.TotalCount, partnersList.TotalPages);
            return partnersList;
        }

        [HttpGet("GetAllPartnersForSelect")]
        public async Task<List<PartnerListForSelectDto>> GetAllPartners()
        {
            return await this._repository.GetAllPartners();
        }

        [HttpGet("GetPartnerByID/{partnerId}")]
        public async Task<PartnerDisplayDto> GetPartnerByID(int partnerId)
        {
            return await this._repository.GetPartnerByID(partnerId);
        }

        [HttpPost("CreatePartner")]
        public async Task<IActionResult> CreatePartner(CreatePartnerDto partner)
        {
            int partnerID = await _repository.CreatePartner(partner);
            return Ok(partnerID);
        }

        [HttpGet("PartnerEmailExists")]
        public async Task<bool> PartnerEmailExists(string email)
        {
            return await _repository.PartnerEmailExists(email);
        }

        [HttpGet("GetPartnerImageByID/{partnerID}")]
        public async Task<IActionResult> GetPartnerImageByID(int partnerID)
        {
            var imageUrl = await _repository.GetPartnerImageByID(partnerID);

            if (string.IsNullOrEmpty(imageUrl))
                return NotFound(new { message = "Image not found" });

            return Ok(new { url = imageUrl });
        }


        [HttpGet("PartnerPhoneExists")]
        public async Task<bool> PartnerPhoneExists(string phone)
        {
            return await _repository.PartnerPhoneExists(phone);
        }

        [HttpPost("UploadPartnerLogo/{partnerID}")]
        public async Task<IActionResult> UploadPartnerLogo(IFormFile imageFile, int partnerID)
        {
            try
            {

                if (Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    await _partnerService.UploadPartnerLogo(file, partnerID);
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        
        
    }
}