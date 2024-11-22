using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class RatingMapperProfile : Profile
    {
        public RatingMapperProfile()
        {
            CreateMap<Rating, RatingListDto>()
              .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
        }
    }
}

