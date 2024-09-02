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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly VoltaXApiDbContext _context;
        private readonly IUserRepository _userRepo;
        private readonly IMailService _mailService;

        public AuthController(
                IAuthRepository repo, 
                IUserRepository userRepo, 
                IConfiguration config, 
                IMailService mailService,
                VoltaXApiDbContext context)
        {
            _repo = repo;
            _config = config;
            _context = context;
            _userRepo = userRepo;
            _mailService = mailService;
        }

        // Registration Method
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserForRegisterDto userForRegisterDto)
        {
            userForRegisterDto.Email = userForRegisterDto.Email.ToLower();
            if (await _repo.UserExists(userForRegisterDto.Email))
            {
                return BadRequest("Email already exists");
            }

            // Creating user
            var userToCreate = new User
            {
                Email = userForRegisterDto.Email,
                FirstName = userForRegisterDto.FirstName,
                LastName = userForRegisterDto.LastName,
                Phone = userForRegisterDto.Phone,
            };

            var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

            MailRequest requ = new MailRequest{
                Phone = "",
                Email = "support@voltaxcharging.com",
                Name = "IQOR Imane",
                ToEmail = createdUser.Email,
                Subject = "Email Verification",
                Body = ""
            };
            await this._mailService.SendVerificationEmailAsync(requ,"http://localhost:4200/auth/verify-email?email="+createdUser.Email+"&token="+createdUser.EmailVerificationToken);

            await _context.SaveChangesAsync();

            return StatusCode(201);
        }


        // Login Method
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Email.ToLower(), userForLoginDto.Password);
            if (userFromRepo == null)
            {
                return Unauthorized();
            }

            var user = await _repo.GetUser(userFromRepo.ID);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.ID.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.Email),
                new Claim(ClaimTypes.GivenName, userFromRepo.FirstName),
                new Claim(ClaimTypes.Surname, userFromRepo.LastName),
                new Claim(ClaimTypes.Role, ((int)userFromRepo.Role).ToString())
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
            // Find the user by their email
            var user = await this._userRepo.FindUserByEmail(userDto.Email);
            if (user == null)
                throw new Exception("User not found");

            // Check that the tokens match
            if (user.ResetPasswordToken != userDto.Token)
                throw new Exception("Invalid token");

            // Update the user's password
            byte[] passHash, passSalt;
            this._repo.CreatePasswordHash(userDto.Password, out passHash, out passSalt); // Make sure to hash the password!
            user.PasswordHash = passHash;
            user.PasswordSalt = passSalt;

            // Invalidate the token so it can't be used again
            user.ResetPasswordToken = null;

            // Update the user in the database
            await this._userRepo.Update(user);
        }

        [HttpGet("ResetPassword")]
        public async Task ResetPasswordRequest([FromQuery] string email)
        {
            Console.WriteLine("Email : "+email);
            string resetToken = await this._userRepo.GenerateResetPasswordTokenForUser(email);
            MailRequest requ = new MailRequest{
                Phone = "",
                Email = "no-reply@voltaxcharging.com",
                Name = "IQOR Imane",
                ToEmail = email,
                Subject = "Password Reset",
                Body = ""
            };
            Console.WriteLine("this is in here brother");
            await this._mailService.SendVerificationEmailAsync(requ,"http://localhost:4200/auth/reset-password?email="+email+"&token="+resetToken);

        }


        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(UserPasswordChangeDto userPasswordChangeDto)
        {
            // Init passwordhash and salt (new ones)
            byte[] passwordHash, passwordSalt;

            // Getting User
            var user = await _repo.GetUser(userPasswordChangeDto.ID);

            // Checking the password
            if (_repo.VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                // Changing The password
                _repo.CreatePasswordHash(userPasswordChangeDto.NewPassword, out passwordHash, out passwordSalt);
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
            if (await _repo.VerifyEmail(verifyEmailDto.Email, verifyEmailDto.Token))
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
            if (await _repo.VerifyPhoneNumber(verifyPhoneDto.Phone, verifyPhoneDto.Token))
            {
                return StatusCode(200);
            }
            else
            {
                return StatusCode(500, "Phone number or token incorrect");
            }
        }
    
        // Send Phone Verification
        [HttpPost("SendPhoneVerificationSMS")]
        public async Task<IActionResult> SendPhoneVerificationSms([FromBody] AddPhoneNumberDto addPhoneNumberDto)
        {
            await this._repo.CreatePhoneVerificationToken(addPhoneNumberDto);

            // sending phone verification token via sms
            return StatusCode(200);
        }

        // Send Email Verification
        [HttpPost("SendEmailVerificationCode")]
        public async Task<IActionResult> SendEmailVerificationCode([FromBody] int userID)
        {
            await this._repo.CreateEmailVerificationToken(userID);

            // sending email verification via email
            return StatusCode(200);
        }
    }

    

}