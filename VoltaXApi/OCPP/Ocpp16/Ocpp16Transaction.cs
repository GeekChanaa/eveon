using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Ocpp16
{
    /// <summary>
    /// The integer transactionId issued to an OCPP 1.6 charger on StartTransaction (= <see cref="Id"/>).
    /// The matching <see cref="VoltaXApi.Models.Transaction"/> (or OCPI session) uses <see cref="Uid"/> as its uid.
    /// </summary>
    public class Ocpp16Transaction
    {
        public const string UidPrefix = "ocpp16-";

        public int Id { get; set; }
        public int ChargePointID { get; set; }
        public int ConnectorId { get; set; }
        [MaxLength(36)]
        public string? IdTag { get; set; }
        public int? ReservationId { get; set; }
        public int MeterStartWh { get; set; }
        public DateTime StartTimestamp { get; set; }
        public int? MeterStopWh { get; set; }
        public DateTime? StopTimestamp { get; set; }
        [MaxLength(40)]
        public string? StopReason { get; set; }
        [MaxLength(20)]
        public string? AuthorizationStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Uid => ToUid(Id);

        public static string ToUid(int transactionId) => UidPrefix + transactionId;

        /// <summary>Accepts "ocpp16-12" as well as the bare charger transactionId "12".</summary>
        public static bool TryParseUid(string? value, out int transactionId)
        {
            transactionId = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var text = value.StartsWith(UidPrefix, StringComparison.OrdinalIgnoreCase) ? value[UidPrefix.Length..] : value;
            return int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out transactionId)
                && transactionId > 0;
        }
    }
}
