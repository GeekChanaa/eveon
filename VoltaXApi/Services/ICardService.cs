using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
  public interface ICardService
  {
    Task<AuthorizationStatusEnumType> ValidateCard(string idTag);
    Task<bool> SubstractAmountFromCardByMinutes(int cardTagID, double minutesCharged, int connectorID, int chargingSessionID);
    Task<bool> SubstractAmountFromCardByIdleMinutes(int cardTagID, double idleMinutes, int connectorID, int chargingSessionID);
    /// <summary>Credits a paid recharge order to its card (once per order).</summary>
    Task<bool> AddAmountToCard(int cardID, decimal amount, int orderID);
  }
}