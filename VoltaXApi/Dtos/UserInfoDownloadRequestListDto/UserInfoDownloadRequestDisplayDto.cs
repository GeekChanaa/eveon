
using VoltaXApi.Models;

namespace VoltaXApi.Dtos;


public class UserInfoDownloadRequestDisplayDto
{
    public int ID { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public DateTime RequestTime { get; set; }
    public DownloadRequestStatusEnum Status { get; set; } 
}