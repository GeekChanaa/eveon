using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class ChargePointCreateDto
    {
        [Required, StringLength(100)]
        public string SerialNumber { get; set; }
        [StringLength(100)]
        public string? Make { get; set; }
        public ChargePointStatusEnum Status { get; set; }
        [StringLength(1000)]
        public string? Comment { get; set; }
        [StringLength(64)]
        public string? Username { get; set; }
        [RegularExpression(@"^[ -~]{16,40}$", ErrorMessage = "Password must be 16-40 printable ASCII characters.")]
        public string? Password { get; set; }
        [StringLength(128)]
        public string? ClientCertThumb { get; set; }
        public ChargePointCategoryEnum Category { get; set; }
        [MaxLength(2)]
        public virtual ICollection<ConnectorCreateDto>? Connectors { get; set; }
    }
}