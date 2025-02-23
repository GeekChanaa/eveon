using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IPartnerRepository : IRepository<Partner>
    {
        IQueryable<PartnerListDto> GetPartners();
        Task CreatePartner(CreatePartnerDto partner);
        Task<PartnerDisplayDto> GetPartnerByID(int id);
    }
}