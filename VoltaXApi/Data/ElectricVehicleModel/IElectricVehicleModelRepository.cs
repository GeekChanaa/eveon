using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IElectricVehicleModelRepository : IRepository<ElectricVehicleModel>
    {
        Task<List<EVModelForSelecttDto>> GetAllElectricVehicleModelsForSelect();
    }
}