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

        // Save user notification setting
        [HttpPost("SaveUserNotificationSetting")]
        public async Task<IActionResult> SaveUserNotificationSetting(NotificationSetting notificationSetting)
        {
            var ns = await this._repository.GetUserNotificationSetting(notificationSetting.UserID, notificationSetting.NotificationTypeID);
            if(ns == null)
            {
                await this._repository.AddAsync(notificationSetting);
            }
            else
            {
                ns.UserID = notificationSetting.UserID;
                ns.NotificationTypeID = notificationSetting.NotificationTypeID;
                ns.Email = notificationSetting.Email;
                ns.Active = notificationSetting.Active;
                ns.Urgent = notificationSetting.Urgent;

                Console.WriteLine("updating : notificationSetting : "+ns.ID);
                await this._repository.Update(ns);
            }
            return StatusCode(200);
        }


    }
}