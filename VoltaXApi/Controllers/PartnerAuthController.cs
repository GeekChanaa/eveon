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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using VoltaXApi.Configurations;

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
        private readonly IGoogleAuthProvider _googleAuthProvider;

        public PartnerAuthController(
                IUserRepository userRepo,
                IConfiguration config,
                IMailService mailService,
                IAuthService authService,
                ILogger<AuthController> logger,
                IMailRequestFactory mailRequestFactory,
                IPartnerAuthService partnerAuthService,
                IGoogleAuthProvider googleAuthProvider,
                ISnsService snsService)
        {
            _googleAuthProvider = googleAuthProvider;
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
            // The partner portal signs in by email only.
            if (string.IsNullOrWhiteSpace(loginDto.Email))
                return BadRequest("An email address is required");

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _partnerAuthService.Login(loginDto.Email.ToLower(), loginDto.Password, ipAddress, userAgent);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }

        // -----------------------------------------------------------------
        // Google sign in (partner portal)
        // -----------------------------------------------------------------

        /// <summary>
        /// Redirect flow for the partner portal. Shares the callback with the customer
        /// portal, the portal itself travels in the (encrypted) OAuth state.
        /// </summary>
        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(action: "GoogleCallback", controller: "Auth")
            };
            properties.Items[ExternalAuthDefaults.PortalItem] = ExternalAuthDefaults.PartnerPortal;

            if (!string.IsNullOrWhiteSpace(returnUrl))
                properties.Items["returnUrl"] = returnUrl;

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Token flow for the partner portal (Google Identity Services button / mobile).
        /// </summary>
        [HttpPost("google")]
        public async Task<IActionResult> GoogleTokenLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            var externalUser = await _googleAuthProvider.ValidateIdTokenAsync(googleLoginDto.IdToken);
            var result = await _partnerAuthService.ExternalLogin(
                externalUser,
                AuthProviderEnum.Google,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString());

            return Ok(result);
        }

    }
}