using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class UpdateUserEmailDto
    {
        public int ID { get; set; }
        public string Email { get; set;}
    }
}