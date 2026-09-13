using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VoltaXApi.Settings;

namespace VoltaXApi.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly AuthTokenSettings _tokenSettings;

    public JwtService(IConfiguration config, IOptions<AuthTokenSettings> tokenSettings)
    {
        _config = config;
        _tokenSettings = tokenSettings.Value;
    }

    public TimeSpan AccessTokenLifetime => _tokenSettings.AccessTokenLifetime;

    public string GenerateToken(List<Claim> claims) => GenerateAccessToken(claims).Token;

    public AccessToken GenerateAccessToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.GetSection("AppSettings:Token").Value));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // UTC throughout: the SPA compares the expiry against Date.now().
        var expires = DateTime.UtcNow.Add(_tokenSettings.AccessTokenLifetime);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            SigningCredentials = creds,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AccessToken(tokenHandler.WriteToken(token), expires);
    }
}
