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
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : GenericController<User>
    {
        private readonly IUserRepository _repository;

        public UserController(IUserRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("UserEmailExists")]
        public async Task<ActionResult<Boolean>> UserEmailExists(string email)
        {
            return await this._repository.UserEmailExists(email);
        }

        [HttpGet("GetUserDebitCards")]
        public async Task<ActionResult<List<DebitCardListingDto>>> GetUserDebitCards([FromQuery] int UserID)
        {
            return await this._repository.GetUserDebitCards(UserID);
        }
    }
}