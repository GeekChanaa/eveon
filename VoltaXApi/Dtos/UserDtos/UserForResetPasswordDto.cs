using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserForResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        [StrongPassword]
        public string Password { get; set; }
    }
}