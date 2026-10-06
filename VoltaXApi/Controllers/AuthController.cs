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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

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
        private readonly ITwoFactorService _twoFactorService;
        private readonly VoltaXApi.ScaleOut.ISecurityStateStore _cache;
        private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;

        private static readonly TimeSpan GoogleLinkTicketLifetime = TimeSpan.FromSeconds(60);

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
                IRefreshTokenService refreshTokenService,
                ITwoFactorService twoFactorService,
                VoltaXApi.ScaleOut.ISecurityStateStore cache,
                IOptionsMonitor<JwtBearerOptions> jwtOptions)
        {
            _twoFactorService = twoFactorService;
            _cache = cache;
            _jwtOptions = jwtOptions;
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
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
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
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> Login(UserForLoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Identifier))
                return BadRequest("An email address or a phone number is required");

            var result = await _authService.Login(loginDto.Identifier, loginDto.Password, ClientIp(), ClientUserAgent());

            if (result == null)
                return Unauthorized();

            return Ok(RefreshTokenCookie.Apply(HttpContext, result));
        }

        /// <summary>
        /// Second step of a sign in on an account with 2FA: the challenge token from the
        /// login response plus a 6 digit TOTP code (or a recovery code).
        /// </summary>
        [HttpPost("verify-2fa")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorLoginDto dto)
        {
            var result = await _authService.CompleteTwoFactorLogin(dto.TwoFactorToken, dto.Code, ClientIp(), ClientUserAgent());
            return Ok(RefreshTokenCookie.Apply(HttpContext, result));
        }


        [HttpPost("CheckToken")]
        public bool ValidateCurrentToken(TokenForValidation token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token.Token,
                    _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters,
                    out SecurityToken validatedToken);
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
        /// Browsers send it in the HttpOnly cookie, mobile clients in the body.
        /// </summary>
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDto? dto)
        {
            try
            {
                var result = await _refreshTokenService.Rotate(RefreshTokenCookie.Read(HttpContext, dto?.RefreshToken), ClientIp(), ClientUserAgent());
                return Ok(RefreshTokenCookie.Apply(HttpContext, result));
            }
            catch (UnauthorizedException)
            {
                RefreshTokenCookie.Clear(HttpContext);
                throw;
            }
        }

        /// <summary>
        /// Ends one session. Answers 200 even for an unknown token: a sign out must never
        /// tell the caller whether a token was real.
        /// </summary>
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequestDto? dto)
        {
            var token = RefreshTokenCookie.Read(HttpContext, dto?.RefreshToken);
            if (token != null)
                await _refreshTokenService.Revoke(token, "logout");
            RefreshTokenCookie.Clear(HttpContext);
            return Ok();
        }

        /// <summary>Ends every session of the signed in account.</summary>
        [Authorize]
        [HttpPost("LogoutEverywhere")]
        public async Task<IActionResult> LogoutEverywhere()
        {
            await _refreshTokenService.RevokeAllForUser(GetCurrentUserId(), "logout-everywhere");
            RefreshTokenCookie.Clear(HttpContext);
            return Ok();
        }

        /// <summary>Sets a new password with the single use, expiring token from the reset email / mobile code.</summary>
        [HttpPost("ResetPassword")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> ResetPassword(UserForResetPasswordDto userDto)
        {
            await _authService.ResetPassword(userDto.Email, userDto.Token, userDto.Password);
            return Ok();
        }

        /// <summary>Always 200, whether or not the address belongs to an account.</summary>
        [HttpPost("request-password-reset")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> RequestPasswordReset([FromBody] EmailRequestDto dto)
        {
            await _authService.ResetPasswordRequest(dto.Email);
            return Ok();
        }

        [HttpGet("ResetPassword")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> ResetPasswordRequest([FromQuery] string email)
        {
            await _authService.ResetPasswordRequest(email);
            return StatusCode(200);
        }

        [HttpGet("ResetPasswordForMobile")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> ResetPasswordRequestForMobile([FromQuery] string email)
        {
            await _authService.ResetPasswordRequestForMobile(email);
            return StatusCode(200);
        }

        /// <summary>Max 5 attempts per code; a correct code returns a fresh single use reset token.</summary>
        [HttpPost("VerifyResetPasswordCodeForMobile")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> VerifyResetPasswordCodeForMobile([FromBody] UserResetPasswordForMobileDto userResetPasswordForMobileDto)
        {
            var resetPasswordToken = await _authService.VerifyResetPasswordCodeForMobile(userResetPasswordForMobileDto);
            return Ok(new
            {
                email = userResetPasswordForMobileDto.Email,
                resetPasswordToken = resetPasswordToken
            });
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
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
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
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
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
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> SendPhoneVerificationSms([FromBody] AddPhoneNumberDto addPhoneNumberDto)
        {
            await this._authService.SendPhoneVerificationToken(addPhoneNumberDto);

            return StatusCode(200);
        }

        [HttpPost("SendEmailVerificationCode")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> SendEmailVerificationCode([FromBody] int userID)
        {
            await this._authService.CreateEmailVerificationToken(userID);

            return StatusCode(200);
        }

        /// <summary>Re-sends the verification link to the signed in user's address (no-op once verified).</summary>
        [Authorize]
        [HttpPost("resend-verification")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> ResendVerification()
        {
            await _authService.ResendVerificationEmail(GetCurrentUserId());
            return Ok();
        }

        // -----------------------------------------------------------------
        // Two factor authentication (TOTP) for admin and partner accounts
        // -----------------------------------------------------------------

        [Authorize]
        [HttpGet("2fa")]
        public async Task<IActionResult> GetTwoFactorStatus()
            => Ok(await _twoFactorService.GetStatus(GetCurrentUserId()));

        /// <summary>Generates a new (pending) secret; returned once as otpauth URI, base32 and QR code.</summary>
        [Authorize]
        [HttpPost("2fa/enroll")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> BeginTwoFactorEnrollment()
            => Ok(await _twoFactorService.BeginEnrollment(GetCurrentUserId()));

        [Authorize]
        [HttpPost("2fa/confirm")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> ConfirmTwoFactorEnrollment([FromBody] TwoFactorCodeDto dto)
            => Ok(new { recoveryCodes = await _twoFactorService.ConfirmEnrollment(GetCurrentUserId(), dto.Code) });

        [Authorize]
        [HttpPost("2fa/disable")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableDto dto)
        {
            await _twoFactorService.Disable(GetCurrentUserId(), dto.Password, dto.Code);
            return Ok();
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
        /// Issues a one time, 60 second ticket for <see cref="GoogleLink"/>: a top level
        /// navigation cannot carry the Authorization header, and the JWT must never travel in a URL.
        /// </summary>
        [Authorize]
        [HttpPost("google/link-ticket")]
        public async Task<IActionResult> CreateGoogleLinkTicket()
        {
            var ticket = AuthHelper.GenerateSecureToken();
            await _cache.SetAsync(GoogleLinkTicketKey(ticket), GetCurrentUserId().ToString(), GoogleLinkTicketLifetime);
            return Ok(new { ticket, expiresIn = (int)GoogleLinkTicketLifetime.TotalSeconds });
        }

        /// <summary>
        /// Starts the redirect flow for an already authenticated user that wants to attach
        /// their Google account. The ticket comes from <see cref="CreateGoogleLinkTicket"/> and is spent here.
        /// </summary>
        [HttpGet("google-link")]
        public async Task<IActionResult> GoogleLink([FromQuery] string ticket, [FromQuery] string? returnUrl = null)
        {
            var userID = await ConsumeGoogleLinkTicket(ticket);
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

                if (login.RequiresTwoFactor)
                    return Redirect(BuildSpaRedirect(portal, twoFactorToken: login.TwoFactorToken, returnUrl: returnUrl));

                // The refresh token goes into the HttpOnly cookie, never into the URL.
                RefreshTokenCookie.Apply(HttpContext, login);
                return Redirect(BuildSpaRedirect(portal, token: login.Token, returnUrl: returnUrl,
                    enrollTwoFactor: login.TwoFactorEnrollmentRequired));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google sign in failed");
                // Only our own, user-facing messages go back to the SPA; anything else stays in the log.
                var error = ex is UnauthorizedException or ValidationException ? ex.Message : "Google sign in failed. Please try again.";
                return Redirect(BuildSpaRedirect(portal, error: error, returnUrl: returnUrl));
            }
        }

        /// <summary>
        /// Token flow used by the mobile app and by the Google Identity Services button:
        /// the client already holds a Google ID token and exchanges it for a VoltaX JWT.
        /// </summary>
        [HttpPost("google")]
        [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
        public async Task<IActionResult> GoogleTokenLogin([FromBody] GoogleLoginDto googleLoginDto)
        {
            var externalUser = await _googleAuthProvider.ValidateIdTokenAsync(googleLoginDto.IdToken);
            var result = await _authService.ExternalLogin(externalUser, AuthProviderEnum.Google, ClientIp(), ClientUserAgent());

            return Ok(RefreshTokenCookie.Apply(HttpContext, result));
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

        private static string GoogleLinkTicketKey(string ticket) => "google-link-ticket:" + AuthHelper.Sha256Hex(ticket);

        private async Task<int?> ConsumeGoogleLinkTicket(string ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return null;

            // Read and delete in one step, so a ticket is spent once even across instances.
            var value = await _cache.TakeAsync(GoogleLinkTicketKey(ticket));
            return int.TryParse(value, out var userID) ? userID : null;
        }

        private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string ClientUserAgent() => Request.Headers["User-Agent"].ToString();

        private string BuildSpaRedirect(string portal, string? token = null, string? twoFactorToken = null, string? error = null, bool linked = false, string? returnUrl = null, bool enrollTwoFactor = false)
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

            if (!string.IsNullOrEmpty(twoFactorToken))
                query.Add("twoFactorToken=" + Uri.EscapeDataString(twoFactorToken));

            if (enrollTwoFactor)
                query.Add("enrollTwoFactor=true");

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
