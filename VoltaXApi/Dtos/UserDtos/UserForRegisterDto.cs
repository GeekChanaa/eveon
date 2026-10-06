using System.ComponentModel.DataAnnotations;
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
        // Version of the terms the user accepted (see the SPA legal pages); stored as proof.
        [Required, StringLength(32)]
        public string TermsVersion { get; set; }
    }
}