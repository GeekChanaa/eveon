using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class CardMapperProfile : Profile
    {
        public CardMapperProfile()
        {
            CreateMap<Card, CardTransactionDto>();
            CreateMap<CreateCardDto, Card>();
            CreateMap<Card, CardWithTransactionsOrdersDto>()
              .ForMember(dest => dest.Orders, opt => opt.MapFrom(src => src.Orders));
        }
    }
}

