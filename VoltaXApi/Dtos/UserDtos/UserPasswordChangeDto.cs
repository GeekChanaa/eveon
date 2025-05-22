using System;
using VoltaXApi.Attributes;

namespace VoltaXApi.Dtos
{
    public class UserPasswordChangeDto
    {
        public int ID { get; set; }
        public string CurrentPassword { get; set; }
        [StrongPassword]
        public string NewPassword { get; set; }
        public string NewPasswordCheck { get; set; }
    }
}