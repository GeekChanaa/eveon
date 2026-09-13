using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
    /// <summary>
    /// Payload used by the "one shot" Google sign in (mobile app / Google Identity Services button).
    /// The client obtains the ID token from Google and posts it here, the API validates it.
    /// </summary>
    public class GoogleLoginDto
    {
        [Required]
        public string IdToken { get; set; }
    }
}
