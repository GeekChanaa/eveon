using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class PartnerMapperProfile : Profile
    {
        public PartnerMapperProfile()
        {
            CreateMap<Partner, PartnerListDto>();
        }
    }
}

