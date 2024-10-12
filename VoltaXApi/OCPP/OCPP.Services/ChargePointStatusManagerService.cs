using System.Collections.Generic;
using System.Net.WebSockets;
using OCPP.Core.Server;

namespace VoltaXApi.OCPP.Services
{

    public class ChargePointStatusManagerService
    {
        private readonly Dictionary<string, ChargePointStatus> _chargePointStatusDict = new Dictionary<string, ChargePointStatus>();

        public void AddOrUpdateChargePointStatus(string chargePointId, ChargePointStatus status)
        {
            _chargePointStatusDict[chargePointId] = status;
        }

        public void RemoveChargePointStatus(string chargePointId)
        {
            _chargePointStatusDict.Remove(chargePointId);
        }

        public ChargePointStatus GetChargePointStatus(string chargePointId)
        {
            _chargePointStatusDict.TryGetValue(chargePointId, out ChargePointStatus status);
            return status;
        }

        public bool ChargePointExists(string chargePointId)
        {
            return _chargePointStatusDict.ContainsKey(chargePointId);
        }

        public Dictionary<string, ChargePointStatus> GetAllChargePointStatuses()
        {
            return new Dictionary<string, ChargePointStatus>(_chargePointStatusDict);
        }

        public void UpdateChargePointStatus(string chargepointIdentifier, ChargePointStatus chargePointStatus)
        {
            if (this.ChargePointExists(chargepointIdentifier))
            {
                var existingStatus = this.GetChargePointStatus(chargepointIdentifier);
                if (existingStatus.WebSocket.State != WebSocketState.Open)
                {
                    this.RemoveChargePointStatus(chargepointIdentifier);
                }
            }
            this.AddOrUpdateChargePointStatus(chargepointIdentifier, chargePointStatus);
        }
    }
}
