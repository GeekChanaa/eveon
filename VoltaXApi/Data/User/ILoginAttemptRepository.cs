using VoltaXApi.Models;
using VoltaXApi.Dtos;
using Microsoft.EntityFrameworkCore;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface ILoginAttemptRepository : IRepository<LoginAttempt>
    {
        Task LoginAttemptFailed(string ipAddress);
    }
}