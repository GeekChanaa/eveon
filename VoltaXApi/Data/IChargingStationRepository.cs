using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IChargingStationRepository : IRepository<ChargingStation>
    {
        new Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID);
    }
}