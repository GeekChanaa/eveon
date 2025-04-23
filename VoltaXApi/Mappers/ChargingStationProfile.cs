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
                .ForMember(dest => dest.ChargingStationImages, opt => opt.MapFrom(src => (object?)null))
                .ForMember(dest => dest.ChargePoints, opt => opt.MapFrom(src => src.ChargePoints));

            CreateMap<ConnectorCreateDto, Connector>();

            CreateMap<ChargePointCreateDto, ChargePoint>()
                .ForMember(dest => dest.Connectors, opt => opt.MapFrom(src => src.Connectors));

            CreateMap<ChargePoint, ChargePointDisplayDto>()
                .ForMember(dest => dest.ChargingStationName, opt => opt.MapFrom(src => src.ChargingStation.Name))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.ChargingStation.Country))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.ChargingStation.State))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.ChargingStation.City))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.ChargingStation.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.ChargingStation.Longitude))
                .ForMember(dest => dest.ChargingStationName, opt => opt.MapFrom(src => src.ChargingStation.Name))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.ChargePointModel.Name))
                .ForMember(dest => dest.ModelImage, opt => opt.MapFrom(src => src.ChargePointModel.ImageUrl));

                


            CreateMap<Image, ImageDto>();
            

            CreateMap<ChargingStation, ChargingStationListDto>()
                .ForMember(dest => dest.ChargePoints, opt => opt.MapFrom(src => src.ChargePoints))
                .ForMember(dest => dest.ChargingStationImages, opt => opt.MapFrom(src => src.ChargingStationImages.Select(i => i.Image)));
                

            CreateMap<ChargePoint, ChargePointListDto>()
                .ForMember(dest => dest.Connectors, opt => opt.MapFrom(src => src.Connectors))
                .ForMember(dest => dest.ChargingStationName, opt => opt.MapFrom(src => src.ChargingStation.Name))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.ChargingStation.Country))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.ChargingStation.State))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.ChargingStation.City))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.ChargingStation.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.ChargingStation.Longitude))
                .ForMember(dest => dest.ModelName, opt => opt.MapFrom(src => src.ChargePointModel.Name))
                .ForMember(dest => dest.ModelImage, opt => opt.MapFrom(src => src.ChargePointModel.ImageUrl));

            CreateMap<Connector, ConnectorListDto>();


        }
    }
}

