using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VoltaXApi.Models;
public class UserInfoDownloadRequest : IEntity
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public DateTime RequestTime { get; set; }
    public DownloadRequestStatusEnum Status { get; set; } 
    public User? User { get; set; } 

    // Archive name inside the private export folder (never a full path, never under wwwroot).
    [MaxLength(128), JsonIgnore]
    public string? ExportFileName { get; set; }
    // SHA-256 of the single-use download token sent by email.
    [MaxLength(128), JsonIgnore]
    public string? DownloadTokenHash { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DownloadedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
