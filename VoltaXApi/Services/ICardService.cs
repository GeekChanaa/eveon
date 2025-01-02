using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public interface ICardService
    {
      Task<AuthorizationStatusEnumType> ValidateCard(string idTag);
    }
}