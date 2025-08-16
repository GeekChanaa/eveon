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

            CreateMap<ChargingSession, ChargingSessionInformationsDto>()
              .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName))
              .ForMember(dest => dest.ChargePointID, opt => opt.MapFrom(src => src.Connector.ChargePoint.ID))
              .ForMember(dest => dest.ChargePointName, opt => opt.MapFrom(src => src.Connector.ChargePoint.ChargePointId))
              .ForMember(dest => dest.TotalPriceWithVAT, opt => opt.MapFrom(src => src.Transactions.Sum(t => t.Amount)))
              .ForMember(dest => dest.KwhCharged, opt => opt.MapFrom(src => src.Transactions.Sum(t => (t.MeterStop ?? 0) - t.MeterStart) / 1000));
        }
    }
}

