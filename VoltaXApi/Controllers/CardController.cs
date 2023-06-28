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
using VoltaXApi.Helpers;
using AutoMapper;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CardController : GenericController<Card>
    {
        private readonly IMapper _mapper;
        private readonly ICardRepository _repository;

        public CardController(ICardRepository repository, IMapper mapper) : base(repository)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet("GetAllCards")]
        public async Task<IActionResult> GetAllCards([FromQuery] GlobalParams globalParams)
        {
            var cards = await PagedList<Card>.CreateAsync((await _repository.GetAllAsync(globalParams)).Include(u => u.User), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(cards.CurrentPage, cards.PageSize, cards.TotalCount, cards.TotalPages);
            List<CardDto> cardsDto = _mapper.Map<List<CardDto>>(cards);
            return Ok(cardsDto);
        }

        // get user recharge cards
        [HttpGet("GetUserRechargeCards")]
        public async Task<ActionResult<List<Card>>> GetUserRechargeCards([FromQuery] int UserID)
        {
            return await this._repository.GetUserRechargeCardsAsync(UserID);
        }
    }
}