using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos
{
    public class CreateCommentDto
    {
        public int ID { get; set; }
        public int? UserID { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [Required, StringLength(2000)]
        public string Text { get; set; }
        public int? ChargingStationID { get; set; }
        public int? ChargePointID { get; set; }
        public int? ConnectorID { get; set; }
        public DateTime? CommentTime { get; set; } = new DateTime();
    }
}