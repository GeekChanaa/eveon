using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IStateRepository : IRepository<State>
    {
        Task<List<string>> GetAllStateNamesByCountry(int countryID);
    }
}