

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;

 public class OCPPDisplayMessageInfo : IEntity
{
    public int ID { get; set; }
    public string ChargePointID { get; set; }
    public int DisplayID { get; set; }
    public int DisplayMessageId { get; set; }
    public int MessageID { get; set; }
    public MessagePriorityEnumType Priority { get; set; }
    public MessageStateEnumType? State { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }  
    public string? TransactionId { get; set; }
    public OCPPDisplayMessageContent? Message { get; set; }
    public OCPPConfigurationComponent? Display { get; set; }
    public ChargePoint? ChargePoint { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}