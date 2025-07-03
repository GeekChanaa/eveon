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
    public class PermissionController : GenericController<Permission>
    {
        private readonly IPermissionRepository _repository;
        public PermissionController(
            IPermissionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetAllPermissions")]
        public async Task<List<PermissionListDto>> GetAllPermissions()
        {
            var permissions = await this._repository.GetAllPermissions();
            return permissions;
        }

        [HttpGet("GetPermissionByID/{permissionID}")]
        public async Task<PermissionDisplayDto> GetPermissionByID(int permissionID)
        {
            var permission = await this._repository.GetPermissionByID(permissionID);
            return permission;
        }

        

    }
}