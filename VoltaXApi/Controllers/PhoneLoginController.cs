using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VoltaXApi.Configurations;
using VoltaXApi.Services;
using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Controllers;
[ApiController]
[Route("api/auth/phone")]
public sealed class PhoneLoginController(IAuthService auth) : ControllerBase
{
    [HttpPost("request")]
    [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
    public async Task<IActionResult> RequestCode(PhoneRequest request)
    {
        try { return Ok(await auth.RequestPhoneLogin(request.Phone, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown")); }
        catch (ArgumentException) { return BadRequest(new { message = "Invalid Moroccan mobile number." }); }
        catch (PhoneLoginRateLimitException) { return StatusCode(429, new { message = "Please wait before requesting another code." }); }
        catch (Amazon.Runtime.AmazonServiceException) { return StatusCode(503, new { message = "SMS delivery is temporarily unavailable. Please try again later." }); }
    }
    [HttpPost("verify")]
    [EnableRateLimiting(SecurityConfiguration.AuthStrictRateLimit)]
    public async Task<IActionResult> VerifyCode(PhoneVerify request)
    {
        try {
            var result = await auth.VerifyPhoneLogin(request.Phone, request.ChallengeId, request.Code, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", Request.Headers.UserAgent);
            return result == null ? Unauthorized(new { message = "Invalid or expired code." }) : Ok(RefreshTokenCookie.Apply(HttpContext, result));
        } catch (ArgumentException) { return BadRequest(new { message = "Invalid Moroccan mobile number." }); }
    }
}
public record PhoneRequest([Required, MaxLength(32)] string Phone);
public record PhoneVerify([Required, MaxLength(32)] string Phone, [Required, MaxLength(32)] string ChallengeId, [Required, RegularExpression(@"^[0-9]{6}$")] string Code);
