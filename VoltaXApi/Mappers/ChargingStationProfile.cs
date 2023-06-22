using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class ChargingStationProfile : Profile
    {
        public ChargingStationProfile()
        {
            CreateMap<ChargingStation, ChargingStationListDto>()
                .ForMember(dest => dest.ChargePoints, opt => opt.MapFrom(src => src.ChargePoints));

            CreateMap<ChargePoint, ChargePointListDto>()
                .ForMember(dest => dest.Connectors, opt => opt.MapFrom(src => src.Connectors));

            CreateMap<Connector, ConnectorListDto>();


        }
    }
}

