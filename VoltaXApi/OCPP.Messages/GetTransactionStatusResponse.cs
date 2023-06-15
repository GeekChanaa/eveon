


using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Messages_OCPP20
{
    public class GetTransactionStatusResponse
    {
        public CustomData CustomData { get; set; }
        public bool? OngoingIndicator { get; set; }

        [Required]
        public bool MessagesInQueue { get; set; }
    }

}