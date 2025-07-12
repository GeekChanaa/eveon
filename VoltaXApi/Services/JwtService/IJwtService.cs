
using System.Security.Claims;

namespace VoltaXApi.Services;

public interface IJwtService
{
    string GenerateToken(List<Claim> claims);
}