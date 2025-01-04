using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class SystemReportMapperProfile : Profile
    {
        public SystemReportMapperProfile()
        {
            CreateMap<SystemReport, SystemReportDisplayDto>()
              .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName))
              .ForMember(dest => dest.CardNumber, opt => opt.MapFrom(src => src.Card.CardNumber))
              .ForMember(dest => dest.ChargePointName, opt => opt.MapFrom(src => src.ChargePoint.ChargePointId))
              .ForMember(dest => dest.ResolvedUserName, opt => opt.MapFrom(src => src.Resolved.FirstName + " " + src.Resolved.LastName))
              .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src => src.Assigned.FirstName + " " + src.Assigned.LastName));

        }
    }
}

