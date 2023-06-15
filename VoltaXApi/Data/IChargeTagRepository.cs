using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IChargeTagRepository : IRepository<ChargeTag>
    {
        Task<ChargeTag?> GetByTagId(string idTag);
    }
}