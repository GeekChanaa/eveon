using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using VoltaXApi.Services;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class PartnerAuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepo;
        private readonly IMailService _mailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IMailRequestFactory _mailRequestFactory;
        private readonly ISnsService _snsService;
        private readonly IPartnerAuthService _partnerAuthService;

        public PartnerAuthController(
                IUserRepository userRepo,
                IConfiguration config,
                IMailService mailService,
                IAuthService authService,
                ILogger<AuthController> logger,
                IMailRequestFactory mailRequestFactory,
                IPartnerAuthService partnerAuthService,
                ISnsService snsService)
        {
            _config = config;
            _userRepo = userRepo;
            _mailService = mailService;
            _logger = logger;
            _mailRequestFactory = mailRequestFactory;
            _snsService = snsService;
            _partnerAuthService = partnerAuthService;
        }

        // Function to reset password
        [HttpGet("ResetPartnerPasswordRequest")]
        public async Task<IActionResult> ResetPartnerPasswordRequest([FromQuery] string email)
        {
            await _partnerAuthService.PartnerResetPasswordRequest(email);
            return StatusCode(200);
        }
        
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto loginDto)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _partnerAuthService.Login(loginDto.Email.ToLower(), loginDto.Password, ipAddress);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }

        
    }
}