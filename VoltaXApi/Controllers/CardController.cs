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
            var cards = await PagedList<Card>.CreateAsync((_repository.GetAllAsync(globalParams)).Include(u => u.User), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(cards.CurrentPage, cards.PageSize, cards.TotalCount, cards.TotalPages);
            List<CardListDto> cardsDto = _mapper.Map<List<CardListDto>>(cards);
            return Ok(cardsDto);
        }

        [HttpGet("GetUserRechargeCards")]
        public async Task<ActionResult<List<Card>>> GetUserRechargeCards([FromQuery] int UserID)
        {
            return await this._repository.GetUserRechargeCardsAsync(UserID);
        }

        [HttpGet("GetCardTransactions")]
        public async Task<ActionResult<List<TransactionDto>>> GetCardTransactions([FromQuery] int CardID)
        {
            return await this._repository.GetCardTransactions(CardID);
        }

        [HttpGet("GetCardOrders")]
        public async Task<ActionResult<List<OrderDto>>> GetCardOrders([FromQuery] int CardID)
        {
            return await this._repository.GetCardOrders(CardID);
        }
        
        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var entity = await this._repository.GetCardByID(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpPost("CreateCard")]
        public async Task<IActionResult> CreateCard(CreateCardDto cardDto)
        {
            await this._repository.CreateCard(cardDto);
            return StatusCode(204);
        }

        [HttpGet("GetCardForDisplayByID/{cardID}")]
        public async Task<IActionResult> GetCardForDisplayByID(int cardID)
        {
            Console.WriteLine("this is in here");
            var card = await this._repository.GetCardForDisplayByID(cardID);
            Console.WriteLine("this is the card balance : " + card.Balance);
            Console.WriteLine("this is the card username : " + card.UserName);
            return Ok(card);
        }
    }
}