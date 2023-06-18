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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly VoltaXApiDbContext _context;

        public AuthController(IAuthRepository repo, IConfiguration config, VoltaXApiDbContext context)
        {
            _repo = repo;
            _config = config;
            _context = context;
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

            // Create link between user and the guest privilege
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
                new Claim(ClaimTypes.Name, userFromRepo.FirstName),
                new Claim(ClaimTypes.Name, userFromRepo.LastName),
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


        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword(UserPasswordChangeDto userPasswordChangeDto)
        {
            // Init passwordhash and salt (new ones)
            byte[] passwordHash,passwordSalt;

            // Getting User
            var user = await _repo.GetUser(userPasswordChangeDto.ID);

            // Checking the password
            if(_repo.VerifyPasswordHash(userPasswordChangeDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                // Changing The password
                _repo.CreatePasswordHash(userPasswordChangeDto.NewPassword, out passwordHash,out passwordSalt);
                user.PasswordSalt = passwordSalt;
                user.PasswordHash = passwordHash;
                await _context.SaveChangesAsync();
            }
            else{
                return StatusCode(500, "Password Incorrect");
            }
            return StatusCode(201);
        }
    }

}