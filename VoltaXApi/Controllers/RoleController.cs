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
    public class RoleController : GenericController<Role>
    {
        private readonly IRoleRepository _repository;
        public RoleController(
            IRoleRepository repository) : base(repository)
        {
            _repository = repository;
        }


        [HttpPost("CreateRole")]
        public async Task<IActionResult> CreateRole(CreateRoleDto role)
        {
            await this._repository.CreateRole(role);
            return StatusCode(200);
        }

        [HttpGet("GetRolesByPermissionID/{permissionID}")]
        public async Task<List<RoleListDto>> GetRolesByPermissionID(int permissionID)
        {
            var roles = await this._repository.GetRolesByPermissionID(permissionID);
            return roles;
        }
        
        [HttpGet("GetAllRoles")]
        public async Task<List<RoleListDto>> GetAllRoles(int permissionID)
        {
            var roles = await this._repository.GetAllRoles();
            return roles;
        }
        
    }
}