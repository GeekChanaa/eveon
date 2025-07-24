using System.Linq.Expressions;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Data
{
    public interface IChargePointModelRepository : IRepository<ChargePointModel>
    {
      Task<int?> GetChargePointModelIDByIdentifier(string identifier);
      Task<List<ChargePointModelSelectDto>> GetAllChargePointModels();
    }
}