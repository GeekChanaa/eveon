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
    Task<bool> SubstractAmountFromCardByMinutes(int cardTagID, double minutesCharged, int connectorID);
    Task<bool> SubstractAmountFromCardByIdleMinutes(int cardTagID, double idleMinutes, int connectorID);
    Task<bool> AddAmountToCard(int cardID, double amount);
  }
}