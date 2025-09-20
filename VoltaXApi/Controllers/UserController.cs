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
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : GenericController<User>
    {
        private readonly IUserRepository _repository;
        private readonly IUserService _userService;

        public UserController(
            IUserRepository repository,
            IUserService userService) : base(repository)
        {
            _repository = repository;
            _userService = userService;
        }

        [HttpPost("CreateUserDashboard")]
        public async Task<ActionResult<int>> CreateUserDashboard(UserDashboardCreateDto userToCreate)
        {
            return await this._userService.CreateUserDashboard(userToCreate);
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

        [HttpGet("GetUserPhoneNumber/{UserID}")]
        public async Task<ActionResult<string>> GetUserPhoneNumber(int UserID)
        {
            return await this._repository.GetUserPhoneNumber(UserID);
        }

        [HttpGet("GetUserDashboardDisplayInformations/{userID}")]
        public async Task<ActionResult<UserDashboardDisplayInformationsDto>> GetUserDashboardDisplayInformations(int UserID)
        {
            return await this._repository.GetUserDashboardDisplayInformations(UserID);
        }

        [HttpPut("EditUserDashboardInformations/{userID}")]
        public async Task EditUserDashboardInformations(int userID, UserDashboardEditInformationsDto userDto)
        {
            await this._userService.EditUserDashboardInformations(userID, userDto);
        }

        [HttpGet("GetUserNames")]
        public async Task<ActionResult<List<UserNameDto>>> GetUserNames()
        {
            return await this._repository.GetUserNames();
        }

        [HttpGet("GetPartnerNames")]
        public async Task<ActionResult<List<UserNameDto>>> GetPartnerNames()
        {
            return await this._repository.GetPartnerNames();
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

        [HttpGet("GetSupportUserNames/")]
        public async Task<ActionResult<List<UserNameDto>>> GetSupportUserNames()
        {
            return await this._repository.GetSupportUserNames();
        }

        [HttpGet("GetUserInformations/{userID}")]
        public async Task<ActionResult<UserListDto>> GetUserInformations(int userID)
        {
            return await this._repository.GetUserInformations(userID);
        }

        [HttpPut("UpdateUserEmail")]
        public async Task<ActionResult<bool>> UpdateUserEmail(UpdateUserEmailDto user)
        {
            return await this._userService.UpdateEmail(user);
        }

        [HttpPut("UpdateUserPhone")]
        public async Task<ActionResult<bool>> UpdateUserPhone(UpdateUserPhoneDto user)
        {
            return await this._userService.UpdatePhone(user);
        }


        [HttpGet("GetUsers")]
        public async Task<List<UserListDto>> GetUsers([FromQuery] GlobalParams globalParams)
        {
            var users = _repository.GetUsers(globalParams);
            var usersList = await PagedList<UserListDto>.CreateAsync(users, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(usersList.CurrentPage, usersList.PageSize, usersList.TotalCount, usersList.TotalPages);
            return usersList;
        }

        [HttpPost("UploadUserAvatar/{userID}")]
        public async Task<IActionResult> UploadUserAvatar(IFormFile imageFile, int userID)
        {
            Console.WriteLine("this is the userController Function");
            try
            {

                if (Request.Form.Files.Count == 1)
                {
                    var file = Request.Form.Files[0];
                    await _userService.UploadUserAvatar(file, userID);
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return BadRequest();
            }
        }
        
        [HttpGet("GetRoleUsers/{roleID}")]
        public async Task<ActionResult<List<UserNameDto>>> GetRoleUsers(int roleID)
        {
            return await this._repository.GetRoleUsers(roleID);
        }
    }
}