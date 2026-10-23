using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    /// <summary>
    /// A payment provider notification that was received and verified. (Provider, EventId) is unique: a
    /// notification delivered twice is processed once.
    /// </summary>
    public class PaymentEvent
    {
        public int ID { get; set; }
        [MaxLength(32)]
        public string Provider { get; set; } = "";
        [MaxLength(128)]
        public string EventId { get; set; } = "";
        [MaxLength(64)]
        public string? Type { get; set; }
        public int? OrderID { get; set; }
        [MaxLength(256)]
        public string? Result { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
