using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace VoltaXApi.Data
{
    public class PartnerRepository : Repository<Partner>,IPartnerRepository
    {
        private readonly IMapper _mapper;
        public PartnerRepository(
            VoltaXApiDbContext context,
            IMapper mapper
            ) : base(context)
        {
            _mapper = mapper;
        }

        public IQueryable<PartnerListDto> GetPartners(GlobalParams globalParams)
        {
            var partners = GetAllAsync(globalParams).ProjectTo<PartnerListDto>(_mapper.ConfigurationProvider);
            return partners;
        }

        public async Task CreatePartner(CreatePartnerDto partner)
        {
            var partnerToAdd = _mapper.Map<CreatePartnerDto,Partner>(partner);
            await AddAsync(partnerToAdd);
        }

        public async Task<PartnerDisplayDto> GetPartnerByID(int id)
        {
            return await _context.Partners.Where(p => p.ID == id).Select(p => new PartnerDisplayDto {
                ID = p.ID.ToString(),
                Name = p.Name,
                Description = p.Description,
                Type = p.Type,
                Email = p.Email,
                Email2 = p.Email2,
                Email3 = p.Email3,
                Phone = p.Phone,
                Phone2 = p.Phone2,
                Phone3 = p.Phone3,
                City = p.City,
                Country = p.Country,
                Address = p.Address,
                TaxIdentificationNumber = p.TaxIdentificationNumber,
                RegistrationNumber = p.RegistrationNumber,
                BankAccountNumber = p.BankAccountNumber,
                LogoUrl = p.Image != null ? p.Image.Url : null
            }).FirstOrDefaultAsync();

        }

        public async Task<bool> PartnerEmailExists(string email)
        {
            return await _context.Partners.AnyAsync(p => p.Email == email || p.Email2 == email || p.Email3 == email);
        }

        public async Task<bool> PartnerPhoneExists(string phone)
        {
            return await _context.Partners.AnyAsync(p => p.Phone == phone || p.Phone2 == phone || p.Phone3 == phone);
        }

    }
}

