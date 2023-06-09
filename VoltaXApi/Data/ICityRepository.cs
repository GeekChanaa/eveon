using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface ICityRepository : IRepository<City>
    {
        Task<List<string>> GetAllCityNamesByCountry(int countryID);
    }
}