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
    public class NotificationTypeController : GenericController<NotificationType>
    {
        private readonly IRepository<NotificationType> _repository;

        public NotificationTypeController(IRepository<NotificationType> repository) : base(repository)
        {
            _repository = repository;
        }

        // getting notification types for some role
        [HttpGet("NotificationTypeFor")]
        public async Task<ActionResult<List<NotificationType>>> GetAllNotificationTypesFor([FromQuery] UserRole role)
        {
            if(role == UserRole.Admin)
            {
                return await this._repository.FindAsync( u => u.ForAdmins == true);
            }
            else
            {
                return await this._repository.FindAsync( u => u.ForCustomers == true);
            }
        }


    }
}