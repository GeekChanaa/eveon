using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using VoltaXApi.Services;
using VoltaXApi.Exceptions;
using VoltaXApi.Factories;
using VoltaXApi.Helpers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using VoltaXApi.Configurations;

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
        private readonly IGoogleAuthProvider _googleAuthProvider;
        private readonly IPartnerAuthService _partnerAuthService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(
                IAuthRepository repo,
                IUserRepository userRepo,
                IConfiguration config,
                IMailService mailService,
                IAuthService authService,
                ILogger<AuthController> logger,
                IMailRequestFactory mailRequestFactory,
                ISnsService snsService,
                IGoogleAuthProvider googleAuthProvider,
                IPartnerAuthService partnerAuthService,
                IRefreshTokenService refreshTokenService)
        {
            _refreshTokenService = refreshTokenService;
            _googleAuthProvider = googleAuthProvider;
            _partnerAuthService = partnerAuthService;
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


        // Login Method, accepts either an email address or a phone number
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Identifier))
                return BadRequest("An email address or a phone number is required");

            var result = await _authService.Login(loginDto.Identifier, loginDto.Password, ClientIp(), ClientUserAgent());

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
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

        /// <summary>
        /// Swaps a refresh token for a new access / refresh pair. The presented token is
        /// spent in the process, so a client must always store the one it gets back.
        /// </summary>
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _refreshTokenService.Rotate(dto.RefreshToken, ClientIp(), ClientUserAgent());
            return Ok(result);
        }

        /// <summary>
        /// Ends one session. Answers 200 even for an unknown token: a sign out must never
        /// tell the caller whether a token was real.
        /// </summary>
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            await _refreshTokenService.Revoke(dto.RefreshToken, "logout");
            return Ok();
        }

        /// <summary>Ends every session of the signed in account.</summary>
        [Authorize]
        [HttpPost("LogoutEverywhere")]
        public async Task<IActionResult> LogoutEverywhere()
        {
            await _refreshTokenService.RevokeAllForUser(GetCurrentUserId(), "logout-everywhere");
            return Ok();
        }

        [HttpPost("ResetPassword")]
        public async Task ResetPassword(UserForResetPasswordDto userDto)
        {
            var user = await this._userRepo.FindUserByEmail(userDto.Email);
            if (user == null)
                throw new NotFoundException("User not found");

            if (string.IsNullOrWhiteSpace(userDto.Token) || user.ResetPasswordToken != userDto.Token)
                throw new ValidationException("Invalid token");

            byte[] passHash, passSalt;
            AuthHelper.CreatePasswordHash(userDto.Password, out passHash, out passSalt); // Make sure to hash the password!
            user.PasswordHash = passHash;
            user.PasswordSalt = passSalt;

            user.ResetPasswordToken = null;

            await this._userRepo.Update(user);

            // The old password is gone, so every session opened with it goes too.
            await _refreshTokenService.RevokeAllForUser(user.ID, "password-reset");
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

        // -----------------------------------------------------------------
        // Google sign in
        // -----------------------------------------------------------------

        /// <summary>
        /// Entry point of the redirect flow. The browser is sent to Google and comes back on
        /// <see cref="GoogleCallback"/>, which hands a JWT back to the SPA.
        /// </summary>
        [HttpGet("google-login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Auth")
            };
            properties.Items[ExternalAuthDefaults.PortalItem] = ExternalAuthDefaults.CustomerPortal;

            if (!string.IsNullOrWhiteSpace(returnUrl))
                properties.Items["returnUrl"] = returnUrl;

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Starts the redirect flow for an already authenticated user that wants to attach
        /// their Google account. The ticket is the user's own JWT.
        /// </summary>
        [HttpGet("google-link")]
        public IActionResult GoogleLink([FromQuery] string ticket, [FromQuery] string? returnUrl = null)
        {
            var userID = ReadUserIdFromTicket(ticket);
            if (userID == null)
                return Redirect(BuildSpaRedirect(ExternalAuthDefaults.CustomerPortal, error: "Your session expired, please sign in again"));

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback), "Auth")
            };
            properties.Items[ExternalAuthDefaults.PortalItem] = ExternalAuthDefaults.CustomerPortal;
            properties.Items[ExternalAuthDefaults.LinkUserIdItem] = userID.Value.ToString();

            if (!string.IsNullOrWhiteSpace(returnUrl))
                properties.Items["returnUrl"] = returnUrl;

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Landing point after Google authenticated the user. Always answers with a redirect
        /// back to the SPA, carrying either a JWT or an error message.
        /// </summary>
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var result = await HttpContext.AuthenticateAsync(ExternalAuthDefaults.ExternalCookieScheme);

            // The temporary cookie has done its job either way.
            await HttpContext.SignOutAsync(ExternalAuthDefaults.ExternalCookieScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                _logger.LogWarning("Google callback reached without a valid external principal: {Failure}", result.Failure?.Message);
                return Redirect(BuildSpaRedirect(ExternalAuthDefaults.CustomerPortal, error: "Google sign in was cancelled or failed"));
            }

            result.Properties.Items.TryGetValue(ExternalAuthDefaults.PortalItem, out var portal);
            portal ??= ExternalAuthDefaults.CustomerPortal;

            result.Properties.Items.TryGetValue("returnUrl", out var returnUrl);

            try
            {
                var externalUser = _googleAuthProvider.FromPrincipal(result.Principal);

                if (result.Properties.Items.TryGetValue(ExternalAuthDefaults.LinkUserIdItem, out var linkUserId)
                    && int.TryParse(linkUserId, out var userID))
                {
                    await _authService.LinkExternalAccount(userID, externalUser, AuthProviderEnum.Google);
                    return Redirect(BuildSpaRedirect(portal, linked: true, returnUrl: returnUrl));
                }

                var login = portal == ExternalAuthDefaults.PartnerPortal
                    ? await _partnerAuthService.ExternalLogin(externalUser, AuthProviderEnum.Google, ClientIp(), ClientUserAgent())
                    : await _authService.ExternalLogin(externalUser, AuthProviderEnum.Google, ClientIp(), ClientUserAgent());

                return Redirect(BuildSpaRedirect(portal, token: login.Token, refreshToken: login.RefreshToken, returnUrl: returnUrl));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google sign in failed");
                return Redirect(BuildSpaRedirect(portal, error: ex.Message, returnUrl: returnUrl));
            }
        }

        /// <summary>
        /// Token flow used by the mobile app and by the Google Identity Services button:
        /// the client already holds a Google ID token and exchanges it for a VoltaX JWT.
        /// </summary>
        [HttpPost("google")]
        public async Task<IActionResult> GoogleTokenLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            var externalUser = await _googleAuthProvider.ValidateIdTokenAsync(googleLoginDto.IdToken);
            var result = await _authService.ExternalLogin(externalUser, AuthProviderEnum.Google, ClientIp(), ClientUserAgent());

            return Ok(result);
        }

        [Authorize]
        [HttpPost("google/link")]
        public async Task<IActionResult> GoogleLinkWithToken([FromBody] GoogleLoginDto googleLoginDto)
        {
            var externalUser = await _googleAuthProvider.ValidateIdTokenAsync(googleLoginDto.IdToken);
            await _authService.LinkExternalAccount(GetCurrentUserId(), externalUser, AuthProviderEnum.Google);

            return Ok(await _authService.GetLinkedAccounts(GetCurrentUserId()));
        }

        [Authorize]
        [HttpPost("google/unlink")]
        public async Task<IActionResult> GoogleUnlink()
        {
            await _authService.UnlinkExternalAccount(GetCurrentUserId(), AuthProviderEnum.Google);

            return Ok(await _authService.GetLinkedAccounts(GetCurrentUserId()));
        }

        [Authorize]
        [HttpGet("linked-accounts")]
        public async Task<IActionResult> GetLinkedAccounts()
        {
            return Ok(await _authService.GetLinkedAccounts(GetCurrentUserId()));
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(value, out var userID))
                throw new UnauthorizedException("Could not resolve the current user");

            return userID;
        }

        /// <summary>
        /// Validates a VoltaX JWT handed over in a query string (the browser cannot set an
        /// Authorization header on a top level navigation) and returns the user it belongs to.
        /// </summary>
        private int? ReadUserIdFromTicket(string ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(ticket, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                        .GetBytes(_config.GetSection("AppSettings:Token").Value)),
                    ValidateIssuer = false,
                    ValidateAudience = false
                }, out _);

                var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out var userID) ? userID : null;
            }
            catch
            {
                return null;
            }
        }

        private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string ClientUserAgent() => Request.Headers["User-Agent"].ToString();

        private string BuildSpaRedirect(string portal, string? token = null, string? refreshToken = null, string? error = null, bool linked = false, string? returnUrl = null)
        {
            string spaLink = _config["SpaLink"];
            if (!spaLink.EndsWith("/"))
                spaLink += "/";

            string path = portal == ExternalAuthDefaults.PartnerPortal
                ? "partner-auth/google-callback"
                : "auth/google-callback";

            var query = new List<string>();

            if (!string.IsNullOrEmpty(token))
                query.Add("token=" + Uri.EscapeDataString(token));

            if (!string.IsNullOrEmpty(refreshToken))
                query.Add("refreshToken=" + Uri.EscapeDataString(refreshToken));

            if (!string.IsNullOrEmpty(error))
                query.Add("error=" + Uri.EscapeDataString(error));

            if (linked)
                query.Add("linked=true");

            if (!string.IsNullOrEmpty(returnUrl))
                query.Add("returnUrl=" + Uri.EscapeDataString(returnUrl));

            // Fragments are not sent to the SPA server or included in referrer headers.
            return spaLink + path + (query.Count > 0 ? "#" + string.Join("&", query) : "");
        }

    }



}
