

using Microsoft.AspNetCore.SignalR;

namespace VoltaXApi.Hubs
{
  [Microsoft.AspNetCore.Authorization.Authorize]
    public class ChargerHub : Hub
  {
      public async Task JoinChargerGroup(string chargerId)
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, chargerId);
          await Clients.Group(chargerId).SendAsync("ReceiveMessage", $"Joined Charger {chargerId}");
      }

      public async Task LeaveChargerGroup(string chargerId)
      {
          await Groups.RemoveFromGroupAsync(Context.ConnectionId, chargerId);
          await Clients.Group(chargerId).SendAsync("ReceiveMessage", $"Left Charger {chargerId}");
      }

      public async Task SendMessageToCharger(string chargerId, string message)
      {
          await Clients.Group(chargerId).SendAsync("ReceiveMessage", message);
      }
  }

}