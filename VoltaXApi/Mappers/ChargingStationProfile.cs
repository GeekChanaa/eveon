using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class ChargingStationProfile : Profile
    {
        public ChargingStationProfile()
        {
            
            CreateMap<ChargingStationCreateDto, ChargingStation>()
            .ForMember(dest => dest.ChargePoints, opt => opt.MapFrom(src => src.ChargePoints));

            CreateMap<ChargePointCreateDto, ChargePoint>()
                .ForMember(dest => dest.Connectors, opt => opt.MapFrom(src => src.Connectors));

            CreateMap<ConnectorCreateDto, Connector>()
                .ForMember(dest => dest.Speed, opt => opt.MapFrom(src => src.Speed));

            CreateMap<Image, ImageDto>();
            

            CreateMap<ChargingStation, ChargingStationListDto>()
                .ForMember(dest => dest.ChargePoints, opt => opt.MapFrom(src => src.ChargePoints))
                .ForMember(dest => dest.ChargingStationImages, opt => opt.MapFrom(src => src.ChargingStationImages.Select(i => i.Image)));
                

            CreateMap<ChargePoint, ChargePointListDto>()
                .ForMember(dest => dest.Connectors, opt => opt.MapFrom(src => src.Connectors));

            CreateMap<Connector, ConnectorListDto>();


        }
    }
}

