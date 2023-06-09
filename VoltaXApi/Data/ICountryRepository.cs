using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface ICountryRepository : IRepository<Country>
    {
        Task<List<string>> GetAllCountryNames();
    }
}