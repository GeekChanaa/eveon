using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface ICityRepository : IRepository<City>
    {
        Task<List<CityNameDto>> GetAllCityNamesByState(int stateID);
    }
}