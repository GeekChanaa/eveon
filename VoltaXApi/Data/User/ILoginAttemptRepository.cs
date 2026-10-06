using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface ILoginAttemptRepository : IRepository<LoginAttempt>
    {
        Task<bool> IsLockedOut(string ipAddress);
        Task<bool> LoginAttemptFailed(string ipAddress);
    }
}