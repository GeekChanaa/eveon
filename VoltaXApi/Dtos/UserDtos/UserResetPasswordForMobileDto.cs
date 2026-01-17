using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserResetPasswordForMobileDto
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}