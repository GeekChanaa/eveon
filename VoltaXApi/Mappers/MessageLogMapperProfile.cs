using AutoMapper;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Mappers
{
    public class MessageLogMapperProfile : Profile
    {
        public MessageLogMapperProfile()
        {
            CreateMap<MessageLog, MessageLogListDto>();
        }
    }

}
