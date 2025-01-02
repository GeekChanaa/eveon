using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class OrderMapperProfile : Profile
    {
        public OrderMapperProfile()
        {
            CreateMap<Order, CardOrderDto>();
        }
    }
}

