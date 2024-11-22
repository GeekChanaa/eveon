using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoltaXApi.Models
{
    public class Rating : IEntity
    {
        public int ID { get; set; }

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        [MaxLength(500)]
        public string Comment { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [Required]
        public string Entity { get; set; }

        [Required]
        public int EntityID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
