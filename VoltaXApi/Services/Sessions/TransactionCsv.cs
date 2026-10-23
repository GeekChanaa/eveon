using System.Globalization;
using System.Text;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;

namespace VoltaXApi.Services.Sessions
{
    /// <summary>OCPP transactions as a semicolon separated file (UTF-8 with BOM, dot decimals, local times).</summary>
    public sealed class TransactionCsv
    {
        private readonly IBusinessClock _clock;

        public TransactionCsv(IBusinessClock clock)
        {
            _clock = clock;
        }

        public byte[] Build(IReadOnlyList<OperatorTransactionDto> transactions)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Transaction;Session;Start;Stop;Station;Charge point;Connector;Meter start (Wh);Meter stop (Wh);Energy (kWh);Id tag;Start result;Stop reason;Status");
            foreach (var t in transactions)
            {
                var energy = t.MeterStop.HasValue ? Math.Max(0, t.MeterStop.Value - t.MeterStart) / 1000 : (double?)null;
                csv.AppendLine(string.Join(";",
                    CsvField.Of(t.Uid), t.ChargingSessionId.ToString(CultureInfo.InvariantCulture),
                    Local(t.StartTime), t.StopTime.HasValue ? Local(t.StopTime.Value) : "",
                    CsvField.Of(t.StationName), CsvField.Of(t.ChargePointName), CsvField.Of(t.Connector),
                    t.MeterStart.ToString("0.##", CultureInfo.InvariantCulture),
                    t.MeterStop?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                    energy?.ToString("0.###", CultureInfo.InvariantCulture) ?? "",
                    CsvField.Of(t.IdTag), CsvField.Of(t.StartResult), CsvField.Of(t.StopReason), t.Status?.ToString() ?? ""));
            }
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        }

        private string Local(DateTime utc) => _clock.ToLocal(utc).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
