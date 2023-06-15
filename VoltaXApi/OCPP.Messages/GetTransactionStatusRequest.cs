

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Messages_OCPP20
{
    public class CustomData
    {
        [Required]
        [MaxLength(255)]
        public string? VendorId { get; set; }
    }

    public class GetTransactionStatusRequest
    {
        public CustomData? CustomData { get; set; }
        
        [Required]
        [MaxLength(36)]
        public string? TransactionId { get; set; }
    }

}