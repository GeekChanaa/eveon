using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
    public interface ICardService
    {
      Task<AuthorizationStatusEnumType> ValidateCard(string idTag);
      Task<bool> SubstractAmountFromCard(int cardTagID, double kwhCharged, int connectorID);
    }
}