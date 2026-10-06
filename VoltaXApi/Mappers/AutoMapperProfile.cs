using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Mappers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<DebitCard, DebitCardListingDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.CardNumberHidden, opt => opt.MapFrom(src => "•••• " + src.Last4))
                .ForMember(dest => dest.NameHidden, opt => opt.MapFrom(src => Mask(src.Name ?? "")));
            
            
           
            CreateMap<Card,CardListDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
            
            CreateMap<Order,OrderDto>();

            CreateMap<CreateReportDto,Report>();
            CreateMap<Report,ReportDisplayDto>()
                .ForMember(dest => dest.ChargePointName, opt => 
                            opt.MapFrom(src => src.ChargePoint != null ? src.ChargePoint.ChargePointId : ""))
                .ForMember(dest => dest.ConnectorName, opt => 
                            opt.MapFrom(src => src.Connector != null ? src.Connector.ConnectorID : 0))
                .ForMember(dest => dest.UserName, opt => 
                            opt.MapFrom(src => src.User != null ? src.User.FirstName + " " +src.User.LastName : ""));
            

            CreateMap<CreateCommentDto,Comment>();
            CreateMap<Comment,CommentDisplayDto>()
                .ForMember(dest => dest.ChargePointName, opt => 
                            opt.MapFrom(src => src.ChargePoint != null ? src.ChargePoint.ChargePointId : ""))
                .ForMember(dest => dest.ConnectorName, opt => 
                            opt.MapFrom(src => src.Connector != null ? src.Connector.ConnectorID : 0))
                .ForMember(dest => dest.ChargingStationName, opt => 
                            opt.MapFrom(src => src.ChargingStation != null ? src.ChargingStation.Name : ""))
                .ForMember(dest => dest.UserName, opt => 
                            opt.MapFrom(src => src.User != null ? src.User.FirstName + " " +src.User.LastName : ""));

            
            CreateMap<Transaction,TransactionDto>();

            CreateMap<ChargePointUptime,ChargePointUptimeListDto>();
            CreateMap<ConnectorUptime,ConnectorUptimeListDto>();


            // Mappers For brands/  Models charge points for seeders
            CreateMap<ChargePointBrandSeederDto,ChargePointBrand>();
            CreateMap<ChargePointModelSeederDto, ChargePointModel>()
                .ForMember(dest => dest.SupportedKwhs, opt => opt.MapFrom(src => 
                    src.SupportedKwhs.Select(kwh => new SupportedKwh
                    {
                        Value = kwh,
                        ChargePointModelID = src.ID
                    }).ToList()));
;

                
        }

        private string Mask(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= 4
                ? new string('X', value.Length)
                : new string('X', value.Length - 4) + value.Substring(value.Length - 4);
        }
    }

}
