using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.OCPP.Messages;
using System.Runtime.CompilerServices;

namespace VoltaXApi.Services
{
  public interface ICardService
  {
    Task<AuthorizationStatusEnumType> ValidateCard(string idTag);
    /// <summary>Credits a paid recharge order to its card (once per order).</summary>
    Task<bool> AddAmountToCard(int cardID, decimal amount, int orderID);
    /// <summary>Raises the high-criticality system report of a card that went below zero.</summary>
    Task HandleCardNegativeBalance(Card card, int connectorID);
  }
}