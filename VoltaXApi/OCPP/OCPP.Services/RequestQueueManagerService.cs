using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VoltaXApi.OCPP.Models;

namespace VoltaXApi.OCPP.Services
{
  public class RequestQueueManagerService
  {
      private readonly ConcurrentDictionary<string, OCPPMessage> _requestQueue = new ConcurrentDictionary<string, OCPPMessage>();

      public bool AddMessage(string key, OCPPMessage message)
      {
          return _requestQueue.TryAdd(key, message);
      }

      public OCPPMessage GetMessage(string key)
      {
          if (_requestQueue.TryGetValue(key, out OCPPMessage message))
          {
              return message;
          }
          return null;
      }

      public bool RemoveMessage(string key)
      {
          return _requestQueue.TryRemove(key, out _);
      }

      public bool ContainsKey(string key)
      {
          return _requestQueue.ContainsKey(key);
      }

      public int GetQueueCount()
      {
          return _requestQueue.Count;
      }

      public void ClearQueue()
      {
          _requestQueue.Clear();
      }
  }

}