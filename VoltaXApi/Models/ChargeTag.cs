
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace VoltaXApi.Models
{
    public class ChargeTag : IEntity
    {
        [Key]
        public int ID { get; set; }
        public string TagID { get; set; }
        public string TagName { get; set; }
        public string ParentTagId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? Blocked { get; set; }
    }
}
