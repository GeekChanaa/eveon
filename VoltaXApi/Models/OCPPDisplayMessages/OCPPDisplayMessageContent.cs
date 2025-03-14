

using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models;
public class OCPPDisplayMessageContent : IEntity
{
    public int ID { get; set; }
    public MessageFormatEnumType Format { get; set; }
    public string? Language { get; set; }    
    public string Content { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}