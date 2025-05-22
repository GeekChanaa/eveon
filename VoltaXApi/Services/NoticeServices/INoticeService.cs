using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface INoticeService
    {
        Task CreateNotice(CreateNoticeDto createNoticeDto);
    }
}