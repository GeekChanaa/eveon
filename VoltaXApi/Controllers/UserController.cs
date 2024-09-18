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
        public async Task<ActionResult<Boolean>> UserEmailExists([FromQuery] string email)
        {
            return await this._repository.UserEmailExists(email);
        }

        [HttpGet("UserPhoneExists")]
        public async Task<ActionResult<Boolean>> UserPhoneExists([FromQuery] string phone)
        {
            return await this._repository.UserPhoneExists(phone);
        }

        [HttpGet("GetUserDebitCards")]
        public async Task<ActionResult<List<DebitCardListingDto>>> GetUserDebitCards([FromQuery] int UserID)
        {
            return await this._repository.GetUserDebitCards(UserID);
        }

        [HttpGet("GetUserNames")]
        public async Task<ActionResult<List<UserNameDto>>> GetUserNames()
        {
            return await this._repository.GetUserNames();
        }

        [HttpGet("GetUserNamesByName")]
        public async Task<ActionResult<List<UserNameDto>>> GetUserNamesByName(string name)
        {
            return await this._repository.GetUserNamesByName(name);
        }

        [HttpGet("IsEmailUnique/{email}")]
        public async Task<ActionResult<bool>> IsEmailUnique(string email)
        {
            return await this._repository.IsEmailUnique(email);
        }

        [HttpGet("IsPhoneUnique/{phone}")]
        public async Task<ActionResult<bool>> IsPhoneUnique(string phone)
        {
            return await this._repository.IsPhoneUnique(phone);
        }
    }
}