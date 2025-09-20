namespace VoltaXApi.Dtos
{
    public class UserSuspendedForMailDto
    {
        public string UserName { get; set; }
        public string SuspensionReason { get; set; }
        public DateTime? SuspendedAt { get; set; }
        
    }
}