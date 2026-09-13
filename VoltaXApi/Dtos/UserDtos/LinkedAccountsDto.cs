namespace VoltaXApi.Dtos
{
    public class LinkedAccountsDto
    {
        public bool GoogleLinked { get; set; }
        public string? GoogleEmail { get; set; }
        public bool HasPassword { get; set; }
        public string AuthProvider { get; set; }
    }
}
