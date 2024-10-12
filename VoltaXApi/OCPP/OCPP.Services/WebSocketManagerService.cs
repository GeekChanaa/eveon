

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace VoltaXApi.OCPP.Services
{
  public class WebSocketManagerService
  {
    private readonly ConcurrentDictionary<string, WebSocket> _webSockets = new ConcurrentDictionary<string, WebSocket>();

    public void AddWebSocket(string chargePointId, WebSocket webSocket)
    {
      _webSockets[chargePointId] = webSocket;
    }

    public void RemoveWebSocket(string chargePointId)
    {
      _webSockets.TryRemove(chargePointId, out _);
    }

    public async Task SendMessageAsync(string chargePointId, string message)
    {
      if (_webSockets.TryGetValue(chargePointId, out WebSocket webSocket) && webSocket.State == WebSocketState.Open)
      {
        var messageBuffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(messageBuffer);
        await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
      }
    }

    public WebSocket GetWebSocket(string chargePointId)
    {
      _webSockets.TryGetValue(chargePointId, out WebSocket webSocket);
      return webSocket;
    }

    public bool GetWebSocketStatus(string chargePointID)
    {
        return _webSockets.ContainsKey(chargePointID);
        
    }
    
    
  }

}