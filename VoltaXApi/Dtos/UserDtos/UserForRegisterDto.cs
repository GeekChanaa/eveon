using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserForRegisterDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [StrongPassword]
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}