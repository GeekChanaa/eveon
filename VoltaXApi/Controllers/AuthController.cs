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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly VoltaXApiDbContext _context;
        private readonly IUserRepository _userRepo;
        private readonly IMailService _mailService;

        public AuthController(
                IAuthRepository repo, 
                IUserRepository userRepo, 
                IConfiguration config, 
                IMailService mailService,
                IAuthService authService,
                VoltaXApiDbContext context)
        {
            _repo = repo;
            _config = config;
            _context = context;
            _userRepo = userRepo;
            _authService = authService;
            _mailService = mailService;
        }

        // Registration Method
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserForRegisterDto userForRegisterDto)
        {
            userForRegisterDto.Email = userForRegisterDto.Email.ToLower();
            string spaLink = _config["SpaLink"];

            if (await _userRepo.UserExists(userForRegisterDto.Email))
            {
                throw new ValidationException("Email already exists");
            }

            // Creating user
            var userToCreate = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                Phone = userForRegisterDto.Phone,
            };

            var createdUser = await _authService.Register(userToCreate, userForRegisterDto.Password);

            MailRequest requ = new MailRequest{
                Phone = "",
                Email = "support@voltaxcharging.com",
                Name = "CHANAA mohammed",
                ToEmails = new List<string>() {createdUser.Email},
                Subject = "Email Verification",
                Body = ""
            };
            await this._mailService.SendVerificationEmailAsync(requ,spaLink+"auth/verify-email?email="+createdUser.Email+"&token="+createdUser.EmailVerificationToken);

            await _context.SaveChangesAsync();

            var userForLogin = new UserForLoginDto{
                Email = userForRegisterDto.Email,
                Password = userForRegisterDto.Password
            };
            return await Login(userForLogin);
        }


        // Login Method
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
                new Claim(ClaimTypes.Role, userFromRepo.Role.ToString())
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
            _authService.CreatePasswordHash(userDto.Password, out passHash, out passSalt); // Make sure to hash the password!
            user.PasswordHash = passHash;
            user.PasswordSalt = passSalt;

            user.ResetPasswordToken = null;

            await this._userRepo.Update(user);
        }

        [HttpGet("ResetPassword")]
        public async Task ResetPasswordRequest([FromQuery] string email)
        {
            string spaLink = _config["SpaLink"];
            string resetToken = await this._userRepo.GenerateResetPasswordTokenForUser(email);
            MailRequest requ = new MailRequest{
                Phone = "",
                Email = "no-reply@voltaxcharging.com",
                Name = "CHANAA mohammed",
                ToEmails = new List<string>{email},
                Subject = "Password Reset",
                Body = ""
            };
            await this._mailService.SendVerificationEmailAsync(requ,spaLink+"auth/reset-password?email="+email+"&token="+resetToken);

        }


        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(UserPasswordChangeDto userPasswordChangeDto)
        {
            // Init passwordhash and salt (new ones)
            byte[] passwordHash, passwordSalt;

            // Getting User
            var user = await _userRepo.GetUser(userPasswordChangeDto.ID);

            // Checking the password
            if (_authService.VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                // Changing The password
                _authService.CreatePasswordHash(userPasswordChangeDto.NewPassword, out passwordHash, out passwordSalt);
                user.PasswordSalt = passwordSalt;
                user.PasswordHash = passwordHash;
                await _context.SaveChangesAsync();
            }
            else
            {
                return StatusCode(500, "Password Incorrect");
            }
            return StatusCode(201);
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
            // Checking the password
            if (await _authService.VerifyPhoneNumber(verifyPhoneDto.Email, verifyPhoneDto.Token))
            {
                return StatusCode(200);
            }
            else
            {
                return StatusCode(500, "Phone number or token incorrect");
            }
        }
    
        [HttpPost("SendPhoneVerificationSMS")]
        public async Task<IActionResult> SendPhoneVerificationSms([FromBody] AddPhoneNumberDto addPhoneNumberDto)
        {
            await this._authService.CreatePhoneVerificationToken(addPhoneNumberDto);

            return StatusCode(200);
        }

        [HttpPost("SendEmailVerificationCode")]
        public async Task<IActionResult> SendEmailVerificationCode([FromBody] int userID)
        {
            await this._authService.CreateEmailVerificationToken(userID);

            return StatusCode(200);
        }
    }

    

}