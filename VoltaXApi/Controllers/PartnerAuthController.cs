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
        private readonly IAuthService _authService;
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
            _authService = authService;
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

        // Login function
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var userFromRepo = await _authService.Login(userForLoginDto.Email.ToLower(), userForLoginDto.Password, ipAddress);

            if (userFromRepo == null)
            {
                return Unauthorized();
            }



            var user = await _userRepo.GetUser(userFromRepo.ID);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.ID.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Email),
                new Claim(ClaimTypes.GivenName, userFromRepo.FirstName),
                new Claim(ClaimTypes.Surname, userFromRepo.LastName),
                new Claim(ClaimTypes.Role, userFromRepo.Role.Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(15),
                SigningCredentials = creds,
            };

            var TokenHandler = new JwtSecurityTokenHandler();
            var token = TokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = TokenHandler.WriteToken(token),
            });
        }

        
        
    }
}