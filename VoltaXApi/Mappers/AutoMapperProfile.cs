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
                .ForMember(dest => dest.CardNumberHidden, opt => opt.MapFrom(src => Mask(src.CardNumber)))
                .ForMember(dest => dest.NameHidden, opt => opt.MapFrom(src => Mask(src.Name)));
            
            
           
            CreateMap<Card,CardListDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FirstName + " " + src.User.LastName));
            
            CreateMap<Order,OrderDto>();
            
            CreateMap<Transaction,TransactionDto>();
                
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
