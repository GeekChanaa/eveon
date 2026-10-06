using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    /// <summary>
    /// A saved payment card. The card number and CVV are never stored: the payment provider keeps
    /// the card and hands back <see cref="ProviderToken"/>; only display metadata lives here.
    /// </summary>
    public class DebitCard  : IEntity
    {
        public int ID { get; set; }
        public int? UserID { get; set; }
        [StringLength(100)]
        public string? Name { get; set; }
        [NotUpdatable]
        public DebitCardTypeEnum Brand { get; set; } = DebitCardTypeEnum.Generic;
        [NotUpdatable, StringLength(4)]
        public string Last4 { get; set; } = string.Empty;
        [NotUpdatable]
        public int ExpiryMonth { get; set; }
        [NotUpdatable]
        public int ExpiryYear { get; set; }
        [NotUpdatable, StringLength(255)]
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string? ProviderToken { get; set; }
        [NotUpdatable, StringLength(50)]
        public string? Provider { get; set; }
        public User? User { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
