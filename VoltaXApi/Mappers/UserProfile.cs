using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Mappers
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserNameDto>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));

            CreateMap<User, UserListDto>();
            CreateMap<User, UserDashboardDisplayInformationsDto>();
            
        }
    }

}
