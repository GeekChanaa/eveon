using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
    public class SuspendUserDto
    {
        // End of the suspension; null suspends the account until it is lifted by hand.
        public DateTime? Until { get; set; }
        [Required, StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; }
        // Whether the user is told by email, with the reason above.
        public bool NotifyUser { get; set; }
    }
}
