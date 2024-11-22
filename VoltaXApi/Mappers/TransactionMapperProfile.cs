using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
namespace VoltaXApi.Mappers
{
    public class TransactionMapperProfile : Profile
    {
        public TransactionMapperProfile()
        {
            
            CreateMap<Transaction, TransactionListDto>();
        }
    }
}

