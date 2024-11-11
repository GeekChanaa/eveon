using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class ChargingSessionProfile : Profile
    {
        public ChargingSessionProfile()
        {
            CreateMap<ChargingSession, ChargePointChargingSessionListDto>()
              .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
        }
    }
}

