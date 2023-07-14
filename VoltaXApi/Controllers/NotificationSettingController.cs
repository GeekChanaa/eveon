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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class NotificationSettingController : GenericController<NotificationSetting>
    {
        private readonly INotificationSettingRepository _repository;

        public NotificationSettingController(INotificationSettingRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // get all notification settings for user
        [HttpGet("UserNotificationSettings/{UserID}")]
        public async Task<ActionResult<List<NotificationSetting>>> GetUserNotificationSettings( int UserID)
        {
            return await this._repository.GetUserNotificationSettings(UserID);
        }


    }
}