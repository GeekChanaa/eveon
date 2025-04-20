using System;

namespace VoltaXApi.Models;
public class UserInfoDownloadRequest : IEntity
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public DateTime RequestTime { get; set; }
    public DownloadRequestStatusEnum Status { get; set; } 
    public User? User { get; set; } 
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}