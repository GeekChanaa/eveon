namespace VoltaXApi.Dtos
{
    public class UserForResetPasswordDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
    }
}