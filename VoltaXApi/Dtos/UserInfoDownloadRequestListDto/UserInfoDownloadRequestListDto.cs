
using VoltaXApi.Models;

namespace VoltaXApi.Dtos;


public class UserInfoDownloadRequestListDto
{
    public int ID { get; set; }
    public string UserName { get; set; }
    public DateTime RequestTime { get; set; }
    public DownloadRequestStatusEnum Status { get; set; } 
}