using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
    public interface IOCPPDisplayMessageInfoRepository : IRepository<OCPPDisplayMessageInfo>
    {
        Task<List<OCPPDisplayMessageInfo>> GetMessagesByChargePointIdAsync(string chargePointId);
        Task<OCPPDisplayMessageInfo> GetMessageByIdAsync(int id, string chargePointId);
        Task<List<OCPPDisplayMessageInfo>> GetActiveMessagesAsync(string chargePointId);
        Task AddMessageAsync(MessageInfoType message, string chargePointId);
        Task AddMessagesAsync(IEnumerable<MessageInfoType> messages, string chargePointId);
        Task UpdateMessageAsync(OCPPDisplayMessageInfo message);
        Task DeleteMessageAsync(int id, string chargePointId);
        Task DeleteExpiredMessagesAsync();
    }
}