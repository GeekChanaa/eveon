using System;
using System.ComponentModel.DataAnnotations;
using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserPasswordChangeDto
    {
        public int ID { get; set; }
        [Required, MaxLength(128)]
        public string CurrentPassword { get; set; }
        [Required, MaxLength(128), StrongPassword]
        public string NewPassword { get; set; }
        [Required, MaxLength(128)]
        public string NewPasswordCheck { get; set; }
    }
}