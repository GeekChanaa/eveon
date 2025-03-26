
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models
{
    public class OCPPLocalListItem  : IEntity
    {
        public int ID { get; set; }
        public string Token { get; set; }
        public IdTokenEnumType TokenType { get; set; }
        public AuthorizationStatusEnumType TokenStatus { get; set; }
        public int OCPPLocalListVersionID { get; set; }
        public OCPPLocalListVersion? OCPPLocalListVersion { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}