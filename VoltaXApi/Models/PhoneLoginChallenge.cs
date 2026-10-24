using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace VoltaXApi.Models;
// The per-IP send limit sums Sends over a recent WindowStart.
[Index(nameof(IpAddress), nameof(WindowStart), Name = "IX_PhoneLoginChallenges_IpAddress")]
public class PhoneLoginChallenge
{
    [Key, MaxLength(16)] public string Phone { get; set; } = "";
    [MaxLength(32)] public string ChallengeId { get; set; } = "";
    [MaxLength(64)] public string Digest { get; set; } = "";
    [MaxLength(64)] public string Salt { get; set; } = "";
    [MaxLength(64)] public string IpAddress { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime ResendAt { get; set; }
    public DateTime WindowStart { get; set; }
    public int Sends { get; set; }
    public int Attempts { get; set; }
    public bool Consumed { get; set; }
}
