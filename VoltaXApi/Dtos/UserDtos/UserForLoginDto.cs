using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
    public class UserForLoginDto
    {
        /// <summary>
        /// Email address. Optional when <see cref="Phone"/> is supplied. Clients that show a
        /// single "email or phone" field may also drop a phone number in here.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Phone number, local ("0610610614") or international ("+212610610614") form.
        /// Optional when <see cref="Email"/> is supplied.
        /// </summary>
        public string? Phone { get; set; }

        [Required]
        public string Password { get; set; }

        /// <summary>
        /// What the user actually typed to identify themselves.
        /// </summary>
        public string? Identifier => string.IsNullOrWhiteSpace(Phone) ? Email : Phone;
    }
}
