using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UpdateUserPhoneDto
    {
        public int ID { get; set; }
        [Required]
        [RegularExpression(@"^\+?[0-9][0-9 ()\-]{5,19}$", ErrorMessage = "Enter a valid phone number.")]
        public string Phone { get; set;}
    }
}