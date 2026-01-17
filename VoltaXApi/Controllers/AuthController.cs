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
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepo;
        private readonly IMailService _mailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IMailRequestFactory _mailRequestFactory;
        private readonly ISnsService _snsService;

        public AuthController(
                IAuthRepository repo,
                IUserRepository userRepo,
                IConfiguration config,
                IMailService mailService,
                IAuthService authService,
                ILogger<AuthController> logger,
                IMailRequestFactory mailRequestFactory,
                ISnsService snsService)
        {
            _repo = repo;
            _config = config;
            _userRepo = userRepo;
            _authService = authService;
            _mailService = mailService;
            _logger = logger;
            _mailRequestFactory = mailRequestFactory;
            _snsService = snsService;
        }

        // Registration Method
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserForRegisterDto userForRegisterDto)
        {
            User user = await this._authService.Register(userForRegisterDto);
            var userForLogin = new UserForLoginDto
            {
                Email = userForRegisterDto.Email,
                Password = userForRegisterDto.Password
            };
            return await Login(userForLogin);
        }


        // Login Method
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto loginDto)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _authService.Login(loginDto.Email.ToLower(), loginDto.Password, ipAddress);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }


        [HttpPost("CheckToken")]
        public bool ValidateCurrentToken(TokenForValidation token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                            .GetBytes(_config.GetSection("AppSettings:Token").Value)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }

        [HttpPost("ResetPassword")]
        public async Task ResetPassword(UserForResetPasswordDto userDto)
        {
            var user = await this._userRepo.FindUserByEmail(userDto.Email);
            if (user == null)
                throw new NotFoundException("User not found");

            if (user.ResetPasswordToken != userDto.Token)
                throw new ValidationException("Invalid token");

            byte[] passHash, passSalt;
            AuthHelper.CreatePasswordHash(userDto.Password, out passHash, out passSalt); // Make sure to hash the password!
            user.PasswordHash = passHash;
            user.PasswordSalt = passSalt;

            user.ResetPasswordToken = null;

            await this._userRepo.Update(user);
        }

        [HttpGet("ResetPassword")]
        public async Task<IActionResult> ResetPasswordRequest([FromQuery] string email)
        {
            await _authService.ResetPasswordRequest(email);
            return StatusCode(200);
        }

        [HttpGet("ResetPasswordForMobile")]
        public async Task<IActionResult> ResetPasswordRequestForMobile([FromQuery] string email)
        {
            await _authService.ResetPasswordRequestForMobile(email);
            return StatusCode(200);
        }

        [HttpPost("VerifyResetPasswordCodeForMobile")]
        public async Task<IActionResult> VerifyResetPasswordCodeForMobile([FromBody] UserResetPasswordForMobileDto userResetPasswordForMobileDto)
        {
            try
            {
                var resetPasswordToken = await _authService.VerifyResetPasswordCodeForMobile(userResetPasswordForMobileDto);
                return Ok(new 
                { 
                    email = userResetPasswordForMobileDto.Email, 
                    resetPasswordToken = resetPasswordToken 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(UserPasswordChangeDto userPasswordChangeDto)
        {
            try
            {
                await _authService.ChangePasswordAsync(userPasswordChangeDto);
                return StatusCode(201);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (IncorrectPasswordException ex)
            {
                return StatusCode(403, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        // Verifying the email
        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto verifyEmailDto)
        {
            // Checking the password
            if (await _authService.VerifyEmail(verifyEmailDto.Email, verifyEmailDto.Token))
            {
                return StatusCode(200);
            }
            else
            {
                return StatusCode(500, "Email or token incorrect");
            }
        }

        // Verifying the phone number
        [HttpPost("VerifyPhone")]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneDto verifyPhoneDto)
        {
            try
            {
                var token = await _authService.VerifyPhoneNumber(verifyPhoneDto.Email, verifyPhoneDto.Token);
                return Ok(new {token = token});
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Phone number or token incorrect");
            }
        }

        [HttpPost("SendPhoneVerificationSMS")]
        public async Task<IActionResult> SendPhoneVerificationSms([FromBody] AddPhoneNumberDto addPhoneNumberDto)
        {
            await this._authService.SendPhoneVerificationToken(addPhoneNumberDto);

            return StatusCode(200);
        }

        [HttpPost("SendEmailVerificationCode")]
        public async Task<IActionResult> SendEmailVerificationCode([FromBody] int userID)
        {
            await this._authService.CreateEmailVerificationToken(userID);

            return StatusCode(200);
        }

        [HttpGet("SendSmsTest/{phone}")]
        public async Task<IActionResult> SendSmsTest(string phone)
        {
            await this._snsService.SendSmsAsync("+212610614476","testing");
            return StatusCode(200);
        }
        
        
    }



}