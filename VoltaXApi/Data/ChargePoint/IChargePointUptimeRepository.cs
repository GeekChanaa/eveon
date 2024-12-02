using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface IChargePointUptimeRepository : IRepository<ChargePointUptime>
    {
        Task StartOnline(string chargePointID); 
        Task StopNormal(string chargePointID); 
    }
}