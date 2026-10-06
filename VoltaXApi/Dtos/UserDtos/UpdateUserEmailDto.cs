using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UpdateUserEmailDto
    {
        public int ID { get; set; }
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set;}
    }
}