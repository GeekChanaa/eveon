

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace VoltaXApi.Services
{
  public class WebSocketManagerService
  {
    // Store WebSocket connections for each ChargePoint ID
    private readonly ConcurrentDictionary<string, WebSocket> _webSockets = new ConcurrentDictionary<string, WebSocket>();

    // Method to add a WebSocket connection
    public void AddWebSocket(string chargePointId, WebSocket webSocket)
    {
      _webSockets[chargePointId] = webSocket;
    }

    // Method to remove a WebSocket connection
    public void RemoveWebSocket(string chargePointId)
    {
      _webSockets.TryRemove(chargePointId, out _);
    }

    // Method to send a message to a WebSocket client
    public async Task SendMessageAsync(string chargePointId, string message)
    {
      if (_webSockets.TryGetValue(chargePointId, out WebSocket webSocket) && webSocket.State == WebSocketState.Open)
      {
        var messageBuffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(messageBuffer);
        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
      }
    }

    // Retrieve WebSocket by chargePointId
    public WebSocket GetWebSocket(string chargePointId)
    {
      foreach(var id in _webSockets){
        Console.WriteLine("chargePointID : "+ id.Key);
        Console.WriteLine("chargePointID Value : "+ id.Value);
      }
      _webSockets.TryGetValue(chargePointId, out WebSocket webSocket);
      return webSocket;
    }
    
    
  }

}