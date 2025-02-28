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
        IQueryable<PartnerListDto> GetPartners(GlobalParams globalParams);
        Task CreatePartner(CreatePartnerDto partner);
        Task<PartnerDisplayDto> GetPartnerByID(int id);
        Task<bool> PartnerEmailExists(string email);
        Task<bool> PartnerPhoneExists(string phone);

    }
}